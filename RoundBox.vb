Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

''' <summary>
''' 常量按 DPI 折算。进程已 DPI 感知，控件的位置和尺寸由 WinForms 按 Dpi 自动放大，
''' 但圆角半径、箭头、内缩这类写死的像素值不会跟着变，必须手动折算，
''' 否则 125% 下圆角看起来会偏小、箭头也偏瘦。
''' </summary>
Friend Module UiDpi
    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Function GetDpiForWindow(hwnd As IntPtr) As UInteger
    End Function

    ''' <summary>
    ''' 取控件所在窗口的物理 DPI。优先问系统（GetDpiForWindow，Win10 1607+），
    ''' 因为 .NET Framework 的 Control.DeviceDpi 在 system 级感知下有时仍返回 96；
    ''' 拿不到才退回 DeviceDpi。老系统没有这个入口，Try 会吞掉 EntryPointNotFound。
    ''' </summary>
    Public Function DpiOf(c As Control) As Integer
        Try
            If c IsNot Nothing AndAlso c.IsHandleCreated Then
                Dim d As Integer = CInt(GetDpiForWindow(c.Handle))
                If d > 0 Then Return d
            End If
        Catch
        End Try
        Try
            If c IsNot Nothing AndAlso c.DeviceDpi > 0 Then Return c.DeviceDpi
        Catch
        End Try
        Return 96
    End Function

    Public Function ScaleOf(c As Control, value As Integer) As Integer
        Return Math.Max(1, CInt(Math.Round(value * DpiOf(c) / 96.0)))
    End Function
End Module

''' <summary>
''' 圆角容器（半径统一 6，与全项目一致）。WinForms 原生 Panel 只有直角，靠自绘补上。
''' 底色（BackColor）设成父容器的颜色，圆角外面才不会留出白边；
''' 圆角内部用 FillColor 填充，可选 1px 描边。
''' </summary>
Friend Class RoundBox
    Inherits Panel

    Public Property Radius As Integer = 6
    Public Property FillColor As Color = Color.White
    Public Property BorderColor As Color = Color.Empty

    ''' <summary>圆角矩形路径，按钮设 Region 时也复用。</summary>
    ''' <remarks>
    ''' 半径必须钳到短边的一半以内：胶囊状控件（Radius = 高度/2）在 DPI 折算后很容易
    ''' 让 radius*2 超过高度，旧版遇到这种情况直接 AddRectangle，圆角就整个消失了。
    ''' 现在钳成半径 = min(radius, 短边\2)，胶囊会自然收成两端半圆。
    ''' </remarks>
    Public Shared Function PathFor(bounds As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim half As Integer = Math.Min(bounds.Width, bounds.Height) \ 2
        Dim r As Integer = Math.Min(Math.Max(1, radius), Math.Max(1, half))
        Dim d As Integer = r * 2
        If bounds.Width < d OrElse bounds.Height < d Then
            path.AddRectangle(bounds)
            Return path
        End If
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90)
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90)
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90)
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim r As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = PathFor(r, UiDpi.ScaleOf(Me, Radius))
            Using b As New SolidBrush(FillColor)
                g.FillPath(b, path)
            End Using
            If BorderColor <> Color.Empty Then
                Using p As New Pen(BorderColor, 1.0F)
                    g.DrawPath(p, path)
                End Using
            End If
        End Using
    End Sub
End Class

''' <summary>
''' 圆角按钮（半径统一 6）。用 Region 裁剪会有锯齿，所以整块自绘：
''' 先用 Surround（周围底色）铺满，再画抗锯齿的圆角填充，最后居中画文字。
''' </summary>
Friend Class RoundButton
    Inherits Button

    Public Property Radius As Integer = 6

    ''' <summary>按钮四周的底色。不在父容器上直接画，否则圆角外会露出父容器的颜色。</summary>
    Public Property Surround As Color = Color.White

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        FlatStyle = FlatStyle.Flat
        FlatAppearance.BorderSize = 0
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Surround)

        ' 禁用态压暗，否则 SetBusy 期间按钮看起来还是能点的
        Dim fill As Color = If(Enabled, BackColor, ControlPaint.Light(BackColor, 0.35F))
        Dim fore As Color = ForeColor

        Using path As GraphicsPath = RoundBox.PathFor(New Rectangle(0, 0, Width - 1, Height - 1), UiDpi.ScaleOf(Me, Radius))
            Using b As New SolidBrush(fill)
                g.FillPath(b, path)
            End Using
        End Using

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, fore,
                              TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or
                              TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPrefix)
    End Sub
End Class
