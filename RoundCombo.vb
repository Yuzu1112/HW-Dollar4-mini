Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

''' <summary>
''' 圆角下拉框。原生 ComboBox 的边框和展开列表都是系统画的，改不了圆角，
''' 所以这里自己画：外壳用 RoundBox 同款圆角，展开层是一个无边框圆角窗体。
''' 对外暴露 Items / SelectedIndex / SelectedIndexChanged，用法和 ComboBox 基本一致。
''' </summary>
Friend Class RoundCombo
    Inherits Control

    ''' <summary>圆角半径。默认 6 与全项目一致（Designer 里各控件也都是 6），别再单独写 4。</summary>
    Public Property Radius As Integer = 6
    Public Property FillColor As Color = Color.White
    Public Property BorderColor As Color = Color.FromArgb(214, 196, 206)
    Public Property ArrowColor As Color = Color.FromArgb(96, 96, 96)

    ''' <summary>控件四周的底色，用于填圆角外侧，避免露白。</summary>
    Public Property Surround As Color = Color.White

    ' 用私有字段 + 只读属性而不是「Property Items As New List」：
    ' 后者是 VB 14 的自动属性初始化器，VS2015 之前的老 vbc 会报 BC36714。
    Private ReadOnly _items As New List(Of String)

    Public ReadOnly Property Items As List(Of String)
        Get
            Return _items
        End Get
    End Property

    Public Event SelectedIndexChanged(sender As Object, e As EventArgs)

    Private _sel As Integer = -1
    Private _hover As Boolean
    Private _popup As ComboPopup

    Public Sub New()
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw Or
                 ControlStyles.Selectable, True)
        TabStop = True
        Cursor = Cursors.Hand
    End Sub

    ''' <summary>兼容 ComboBox 的批量填充接口，Mini 的填充逻辑不用改。</summary>
    Public Sub BeginUpdate()
    End Sub

    Public Sub EndUpdate()
        If _sel < 0 AndAlso Items.Count > 0 Then _sel = 0
        Invalidate()
    End Sub

    Public Property SelectedIndex As Integer
        Get
            Return _sel
        End Get
        Set(value As Integer)
            If value < 0 OrElse value >= Items.Count Then value = -1
            If _sel = value Then Return
            _sel = value
            Invalidate()
            RaiseEvent SelectedIndexChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Public ReadOnly Property SelectedItem As String
        Get
            If _sel >= 0 AndAlso _sel < Items.Count Then Return Items(_sel)
            Return ""
        End Get
    End Property

    ' ---------- 绘制 ----------

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(Surround)

        ' 局部变量别叫 radius —— VB 不区分大小写，会遮蔽同名属性 Radius，
        ' 变成 Dim radius = ScaleOf(Me, radius) 的自引用，读到的永远是 0，圆角就画成直角了。
        Dim cornerR As Integer = UiDpi.ScaleOf(Me, Radius)
        Dim enabledFill As Color = If(Enabled, FillColor, Blend(FillColor, 0.55F))
        Dim rc As New Rectangle(0, 0, Width - 1, Height - 1)
        Using path As GraphicsPath = RoundBox.PathFor(rc, cornerR)
            Using b As New SolidBrush(enabledFill)
                g.FillPath(b, path)
            End Using
            Using p As New Pen(BorderColor, 1.0F)
                g.DrawPath(p, path)
            End Using
        End Using

        ' 当前项文字：左边距 11、右侧给箭头留 37（都按 DPI 折算）
        Dim padL As Integer = UiDpi.ScaleOf(Me, 11)
        Dim padR As Integer = UiDpi.ScaleOf(Me, 37)
        Dim textRect As New Rectangle(padL, 0, Math.Max(8, Width - padL - padR), Height)
        Dim fore As Color = If(Enabled, ForeColor, Color.FromArgb(150, 150, 150))
        TextRenderer.DrawText(g, SelectedItem, Font, textRect, fore,
                              TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                              TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPrefix)

        ' 下拉箭头：实心小三角，中心对齐右侧 17px 处
        Dim cx As Integer = Width - UiDpi.ScaleOf(Me, 17)
        Dim cy As Integer = Height \ 2
        Dim ax As Integer = UiDpi.ScaleOf(Me, 4)
        Dim ay As Integer = UiDpi.ScaleOf(Me, 2)
        Dim arrow As Point() = {
            New Point(cx - ax, cy - ay),
            New Point(cx + ax, cy - ay),
            New Point(cx, cy + UiDpi.ScaleOf(Me, 3))
        }
        Using b As New SolidBrush(If(Enabled, ArrowColor, Color.FromArgb(170, 170, 170)))
            g.FillPolygon(b, arrow)
        End Using

        ' 有焦点时画一圈内缩虚线，键盘用户才知道当前焦点在这
        If Focused AndAlso Enabled Then
            Dim off As Integer = UiDpi.ScaleOf(Me, 2)
            Dim inner As New Rectangle(off, off, Width - off * 2 - 1, Height - off * 2 - 1)
            Using path As GraphicsPath = RoundBox.PathFor(inner, Math.Max(1, cornerR - 1))
                Using p As New Pen(Color.FromArgb(150, 110, 30), 1.0F)
                    p.DashStyle = DashStyle.Dot
                    g.DrawPath(p, path)
                End Using
            End Using
        End If

    End Sub

    Private Shared Function Blend(c As Color, ratio As Single) As Color
        Return Color.FromArgb(
            CInt(c.R + (255 - c.R) * ratio),
            CInt(c.G + (255 - c.G) * ratio),
            CInt(c.B + (255 - c.B) * ratio))
    End Function

    ' ---------- 交互 ----------

    Protected Overrides Sub OnMouseEnter(e As EventArgs)
        MyBase.OnMouseEnter(e)
        _hover = True
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        _hover = False
        Invalidate()
    End Sub

    Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If Not Enabled Then Return
        Focus()
        ToggleDropDown()
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If Not Enabled Then Return
        Select Case e.KeyCode
            Case Keys.Down
                If e.Alt Then
                    ToggleDropDown()
                Else
                    MoveSelection(1)
                End If
                e.Handled = True
            Case Keys.Up
                If e.Alt Then
                    ToggleDropDown()
                Else
                    MoveSelection(-1)
                End If
                e.Handled = True
            Case Keys.Space, Keys.Enter
                ToggleDropDown()
                e.Handled = True
            Case Keys.Escape
                CloseDropDown()
                e.Handled = True
        End Select
    End Sub

    Private Sub MoveSelection(delta As Integer)
        If Items.Count = 0 Then Return
        Dim nextIndex As Integer = _sel + delta
        If nextIndex < 0 Then nextIndex = 0
        If nextIndex > Items.Count - 1 Then nextIndex = Items.Count - 1
        SelectedIndex = nextIndex
    End Sub

    Public Sub ToggleDropDown()
        If _popup IsNot Nothing AndAlso _popup.Visible Then
            CloseDropDown()
        Else
            OpenDropDown()
        End If
    End Sub

    Private Sub OpenDropDown()
        If Items.Count = 0 Then Return
        CloseDropDown()
        _popup = New ComboPopup(Items, _sel, Width, Font, FillColor, BorderColor, Surround, UiDpi.DpiOf(Me), Radius)
        AddHandler _popup.ItemPicked, AddressOf OnPopupPicked
        AddHandler _popup.PopupClosed, AddressOf OnPopupClosed
        _popup.ShowFor(Me)
    End Sub

    Public Sub CloseDropDown()
        If _popup IsNot Nothing Then
            Dim p As ComboPopup = _popup
            _popup = Nothing
            p.Hide()
            p.Dispose()
        End If
    End Sub

    Private Sub OnPopupPicked(index As Integer)
        SelectedIndex = index
    End Sub

    Private Sub OnPopupClosed()
        _popup = Nothing
        Invalidate()
    End Sub

    Protected Overrides Sub OnEnabledChanged(e As EventArgs)
        MyBase.OnEnabledChanged(e)
        If Not Enabled Then CloseDropDown()
        Invalidate()
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then CloseDropDown()
        MyBase.Dispose(disposing)
    End Sub
End Class


''' <summary>
''' 下拉展开层。用无边框窗体托一个自绘列表，圆角靠 Region 裁 + 内部再画一圈抗锯齿圆角，
''' 这样外轮廓是圆的，边缘也不会出现直角台阶。
''' </summary>
Friend Class ComboPopup
    Inherits Form

    Public Event ItemPicked(index As Integer)
    ''' <summary>不能叫 Closed：Form 自带同名事件，重新声明会 shadow 基类并报警告。</summary>
    Public Event PopupClosed()

    Private ReadOnly _items As List(Of String)
    Private _hover As Integer = -1
    Private _closing As Boolean

    ' 行高、内边距、圆角半径都按 DPI 折算，125% 下才不至于挤成一团
    Private ReadOnly _itemH As Integer
    Private ReadOnly _padV As Integer
    Private ReadOnly _radius As Integer
    Private ReadOnly _inset As Integer

    Private ReadOnly _fill As Color
    Private ReadOnly _border As Color
    Private ReadOnly _surround As Color

    Public Sub New(items As List(Of String), selected As Integer, width As Integer,
                   font As Font, fill As Color, border As Color, surround As Color, dpi As Integer,
                   Optional radius As Integer = 6)
        _items = items
        _fill = fill
        _border = border
        _surround = surround
        _hover = selected

        Dim k As Single = If(dpi > 0, dpi / 96.0F, 1.0F)
        _itemH = Math.Max(20, CInt(Math.Round(28 * k)))
        _padV = Math.Max(2, CInt(Math.Round(4 * k)))
        ' 弹层圆角跟下拉框本体走同一个 Radius —— 之前这里写死 4，比本体小，
        ' 弹出来两个圆角对不齐，看着就「有个控件特别圆」。
        _radius = Math.Max(2, CInt(Math.Round(radius * k)))
        _inset = Math.Max(2, CInt(Math.Round(4 * k)))

        FormBorderStyle = FormBorderStyle.None
        ShowInTaskbar = False
        StartPosition = FormStartPosition.Manual
        BackColor = surround
        Font = font
        DoubleBuffered = True
        ' 必须关掉字体自动缩放：否则 ClientSize 会被按 96dpi 比例改写，
        ' 而 Region 用的是构造时算出的高度，两者对不上，底部两个角的圆角就丢了
        AutoScaleMode = AutoScaleMode.None
        SetStyle(ControlStyles.UserPaint Or ControlStyles.AllPaintingInWmPaint Or
                 ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)

        Dim h As Integer = items.Count * _itemH + _padV * 2
        ClientSize = New Size(width, h)
        ApplyRoundRegion()
    End Sub

    ''' <summary>
    ''' 圆角 Region 必须按控件真实尺寸重算。进程已 DPI 感知，Width/Height 就是物理像素，
    ''' Region 也按物理像素走，两边一致，不需要再做 DPI 换算。
    ''' </summary>
    Private Sub ApplyRoundRegion()
        If Width <= 0 OrElse Height <= 0 Then Return
        Dim old As Region = Region
        Region = New Region(RoundBox.PathFor(New Rectangle(0, 0, Width, Height), _radius))
        If old IsNot Nothing Then old.Dispose()
    End Sub

    Protected Overrides Sub OnResize(e As EventArgs)
        MyBase.OnResize(e)
        ApplyRoundRegion()
    End Sub

    ''' <summary>构造时还没有句柄，尺寸可能被系统改写，句柄建好后必须再算一次。</summary>
    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        ApplyRoundRegion()
    End Sub

    Public Sub ShowFor(owner As Control)
        Dim anchor As Point = owner.PointToScreen(New Point(0, owner.Height + 2))
        ' 下方放不下就翻到控件上方
        Dim scr As Rectangle = Screen.FromControl(owner).WorkingArea
        Dim top As Integer = anchor.Y
        If top + Height > scr.Bottom Then top = owner.PointToScreen(New Point(0, -Height - 2)).Y
        Location = New Point(anchor.X, top)
        Show(owner.FindForm())
        BringToFront()
        Activate()
        Invalidate()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.Clear(_surround)

        ' 外圈用边框色铺满圆角，再把填充色内缩 1px 画上去 —— 相当于 1px 描边
        Dim outer As New Rectangle(0, 0, Width, Height)
        Using path As GraphicsPath = RoundBox.PathFor(outer, _radius)
            Using b As New SolidBrush(_border)
                g.FillPath(b, path)
            End Using
        End Using
        Dim inner As New Rectangle(1, 1, Width - 2, Height - 2)
        Using path As GraphicsPath = RoundBox.PathFor(inner, Math.Max(1, _radius - 1))
            Using b As New SolidBrush(_fill)
                g.FillPath(b, path)
            End Using
        End Using

        For i As Integer = 0 To _items.Count - 1
            Dim row As New Rectangle(_inset, _padV + i * _itemH, Width - _inset * 2, _itemH)
            If i = _hover Then
                Using path As GraphicsPath = RoundBox.PathFor(row, Math.Max(2, _radius - 1))
                    Using b As New SolidBrush(Color.FromArgb(246, 224, 238))
                        g.FillPath(b, path)
                    End Using
                End Using
            End If
            Dim indent As Integer = Math.Max(7, CInt(Math.Round(_inset * 1.8)))
            Dim tr As New Rectangle(row.X + indent, row.Y, row.Width - indent - _inset, row.Height)
            TextRenderer.DrawText(g, _items(i), Font, tr, Color.FromArgb(60, 60, 60),
                                  TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or
                                  TextFormatFlags.EndEllipsis Or TextFormatFlags.NoPrefix)
        Next
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        Dim idx As Integer = IndexAt(e.Y)
        If idx <> _hover Then
            _hover = idx
            Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        Dim idx As Integer = IndexAt(e.Y)
        If idx >= 0 Then
            RaiseEvent ItemPicked(idx)
        End If
        ClosePopup()
    End Sub

    Private Function IndexAt(y As Integer) As Integer
        Dim i As Integer = (y - _padV) \ _itemH
        If i < 0 OrElse i >= _items.Count Then Return -1
        Return i
    End Function

    Protected Overrides Sub OnDeactivate(e As EventArgs)
        MyBase.OnDeactivate(e)
        ClosePopup()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            ClosePopup()
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub ClosePopup()
        ' 失活与点选可能连续触发，防重入
        If _closing Then Return
        _closing = True
        Hide()
        RaiseEvent PopupClosed()
        Dispose()
    End Sub
End Class
