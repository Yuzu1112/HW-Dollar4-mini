' HW Dollar4 mini 主窗体 —— 交互逻辑。
'
' 设计取向：不做「把配置读进来编辑再写回」，只做「把文件丢进去、换一种形态吐出来」。
' 界面上只有两块：配置文件（文件 / 密钥库 / 目标格式 → 解密·加密）和密文字段（输入 / 输出），
' 最底下一条单行只读提示，输出最近一步的结果。
Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

Public Class MiniForm

    ''' <summary>当前选中的待处理文件。拖入多个时全部保留，按顺序逐个处理。</summary>
    Private ReadOnly CfgFiles As New List(Of String)

    ''' <summary>忙碌标志：处理期间禁用所有输入，防止重复点击。</summary>
    Private Busy As Boolean = False

    ' 输出框空着时用占位文字；白底输入框靠灰/黑区分「占位」和「真值」，不另存状态
    ' 结果框不放任何提示文字：空就是空，占位色仍用于区分「有值/无值」
    Private Const OutPlaceholder As String = ""
    Private ReadOnly OutNormalColor As Color = Color.Black
    Private ReadOnly OutPlaceColor As Color = Color.Gray

    ' ---------- 系统菜单：开源软件使用声明 / 关于 ----------
    '
    ' 窗体是 FixedSingle，没有菜单栏，标题栏右键的「系统菜单」就是唯一合适的挂点，
    ' 与 HW Dollar4 Lite 保持一致（同样挂在 &H1FFE / &H1FFF 两个自定义命令 ID 上）。

    Private Const WM_SYSCOMMAND As Integer = &H112
    Private Const SC_MAXIMIZE As Integer = &HF030
    Private Const ID_OSS As Integer = &H1FFE
    Private Const ID_ABOUT As Integer = &H1FFF

    <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function AppendMenu(hMenu As IntPtr, uFlags As MenuFlags, uIDNewItem As UIntPtr, lpNewItem As String) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto, CallingConvention:=CallingConvention.Winapi, SetLastError:=True)>
    Private Shared Function GetSystemMenu(hWnd As IntPtr, <MarshalAs(UnmanagedType.Bool)> bRevert As Boolean) As IntPtr
    End Function

    <Flags()>
    Private Enum MenuFlags As Integer
        MF_STRING = 0
        MF_SEPARATOR = &H800
    End Enum

    Private Sub BuildSystemMenu()
        Try
            Dim hMenu As IntPtr = GetSystemMenu(Handle, False)
            If hMenu = IntPtr.Zero Then Return
            AppendMenu(hMenu, MenuFlags.MF_SEPARATOR, New UIntPtr(CUInt(&H1FFD)), "SEPARATOR")
            AppendMenu(hMenu, MenuFlags.MF_STRING, New UIntPtr(CUInt(ID_OSS)), "开源软件使用声明(&O)")
            AppendMenu(hMenu, MenuFlags.MF_STRING, New UIntPtr(CUInt(ID_ABOUT)), "关于(&A)")
        Catch ex As Exception
            ' 系统菜单加不上不影响主流程，但别静默吞掉，调试时至少看得到
            System.Diagnostics.Debug.WriteLine("[忽略] " & ex.GetType().Name & ": " & ex.Message)
        End Try
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_SYSCOMMAND Then
            ' 低 4 位是系统保留标志位，必须掩掉再比较
            Dim cmd As Integer = m.WParam.ToInt32() And &HFFF0
            If cmd = SC_MAXIMIZE Then
                ' 最大化一律挡掉：按钮已由 MaximizeBox=False 去掉，
                ' 但 Aero Snap / 键盘 / 系统菜单仍可能把命令送进来，这里直接吞掉
                Return
            End If
            If cmd = ID_OSS Then
                ShowOssNotice()
                Return
            End If
            If cmd = ID_ABOUT Then
                ShowAbout()
                Return
            End If
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub ShowOssNotice()
        Dim s As String = "此软件包含的开源软件由权利持有人发放许可证。" & vbCrLf & vbCrLf &
                          "软件： aescrypt2 1.0" & vbCrLf &
                          "版权声明： Copyright (C) 2004,2005  Christophe Devine" & vbCrLf &
                          "许可证： GPL v2 License" & vbCrLf &
                          "许可证文本： https://www.gnu.org/licenses/gpl-2.0.txt" & vbCrLf & vbCrLf &
                          "软件： Mbed TLS 3.6.0 LTS" & vbCrLf &
                          "版权声明： (C) Copyright The Mbed TLS Contributors" & vbCrLf &
                          "许可证： GPL v2 License" & vbCrLf &
                          "许可证文本： https://www.gnu.org/licenses/gpl-2.0.txt" & vbCrLf & vbCrLf &
                          "软件： HW-Patrina" & vbCrLf &
                          "项目地址： https://github.com/ipatrina/HW-Patrina" & vbCrLf &
                          "许可证： GPL-2.0 License" & vbCrLf &
                          "许可证文本： https://www.gnu.org/licenses/gpl-2.0.txt"
        MsgBox(s, vbInformation, "开源软件使用声明")
    End Sub

    Private Sub ShowAbout()
        MsgBox("HW Dollar4 mini" & vbCrLf & vbCrLf &
               "华为 ONT 配置文件实用工具（精简版）" & vbCrLf & vbCrLf &
               "软件版本：" & Version3() & vbCrLf &
               "更新时间：" & YearMonthText() & vbCrLf & vbCrLf &
               "Copyright © 2020-20" & YearShort() & " 版权所有", vbInformation, "关于")
    End Sub

    ''' <summary>取版本号前三段，缺失就原样返回，别让关于框空着。</summary>
    Private Function Version3() As String
        Dim p As String() = Application.ProductVersion.Split("."c)
        If p.Length >= 3 Then Return p(0) & "." & p(1) & "." & p(2)
        Return Application.ProductVersion
    End Function

    ''' <summary>版本号第 4 段是 YYMM，返回「20YY」。取不到返回 "----"。</summary>
    Private Function YearShort() As String
        Dim p As String() = Application.ProductVersion.Split("."c)
        If p.Length >= 4 AndAlso p(3).Length >= 2 Then Return p(3).Substring(0, 2)
        Return "----"
    End Function

    ''' <summary>版本号第 4 段 YYMM → 「20YY年M月」，与 Lite 的写法一致。</summary>
    Private Function YearMonthText() As String
        Dim p As String() = Application.ProductVersion.Split("."c)
        If p.Length >= 4 AndAlso p(3).Length >= 4 Then
            Return "20" & p(3).Substring(0, 2) & "年" & CInt(p(3).Substring(2, 2)) & "月"
        End If
        Return "-"
    End Function

    ' ---------- 初始化 ----------

    Private Sub MiniForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Dim ico As Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            If ico IsNot Nothing Then Me.Icon = ico
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("[忽略] 取图标失败: " & ex.Message)
        End Try

        ' 最大化彻底禁用：设计器里已是 FixedSingle + MaximizeBox=False，
        ' 这里再写一次，防止界面被重新生成后属性被覆盖
        MaximizeBox = False

        FillTargetCombo()
        SetVersionText()
        BuildSystemMenu()

        ' 提示一律三个字以内，长句塞进 32px 的框里只会糊成一片
        ' 第一行不写「配置文件」——卡片标题已经说了，写「拖入文件」才是有用的信息
        SetCue(TxtCfgFile, "导入文件")
        SetCue(TxtKmc, "导入 KSF")
        SetCue(TxtPass, "下载密钥")
        SetCue(TxtFieldIn, "密文")
        ShowOutput("")
        SetMsg("")
        UpdateTypeTag(0)

        ' 密钥库若就在程序目录，直接填上，省得每次点浏览
        Dim auto As String = FindLocalKeyStore()
        If auto <> "" Then TxtKmc.Text = auto

        ' $4 解密失败时由内核回调到这里，弹窗要密码
        HwCtx.PasswordProvider = AddressOf AskPassword

    End Sub

    ''' <summary>
    ''' 标题里的版本号从程序集读，规则与 HW Dollar4 Lite 一致：
    ''' AssemblyVersion 四段是 主.次.修订.YYMM，第 4 段拆成 (20YY.MM)。
    ''' 不写死在这里，否则改一次版本要改两处，迟早对不上。
    ''' </summary>
    Private Sub SetVersionText()
        Dim parts As String() = Application.ProductVersion.Split("."c)
        If parts.Length >= 4 Then
            Dim yymm As String = parts(3)
            If yymm.Length >= 4 Then
                LblTitle.Text = "HW Dollar4 mini 版本: " & parts(0) & "." & parts(1) & "." & parts(2) &
                                " (20" & yymm.Substring(0, 2) & "." & yymm.Substring(2, 2) & ")"
                Return
            End If
        End If
        ' 版本号不是四段就原样显示，别让标题空着
        LblTitle.Text = "HW Dollar4 mini 版本: " & Application.ProductVersion
    End Sub

    ' ---------- 占位提示 ----------

    Private Const EM_SETCUEBANNER As Integer = &H1501

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As String) As IntPtr
    End Function

    ''' <summary>TextBox 没有原生占位符，用系统的 Cue Banner 顶上，免得预置假文字。</summary>
    Private Sub SetCue(tb As TextBox, text As String)
        Try
            SendMessage(tb.Handle, EM_SETCUEBANNER, CType(1, IntPtr), text)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("[忽略] 设置占位符失败: " & ex.Message)
        End Try
    End Sub

    ' ---------- 目标格式下拉 ----------

    ' 名称一律极简：下拉框只有 210px，长名字会被截断，也没必要在下拉里讲用法
    '
    ' 本下拉只服务「加密」按钮 —— 选的是「把这个文件加密成什么格式」。
    ' 解密按钮完全不读这里：cmd = "dec" 直接出明文，跟 Target 无关。
    ' 别把文件类型识别名（右上角胶囊标签那套「01 静态容器」）混进来 —— 那是
    ' HwContainer.DetectFormat 回答的「这个文件是什么」，跟这里「要加密成什么」是两个维度。
    '
    ' 命名两段式：
    '   前缀 $2 / $4 = 算法家族（$2 = aescrypt2，$4 = AES-GCM）
    '   后缀 Static / Dynamic = 钥匙来源（Static = 程序内固定常量，Dynamic = 钥匙随包走）
    '
    '   $2 Static  = 01 头，aescrypt2 + gzip，钥匙是 36 字节字面量 hex:13395537…（Keystore D4K1）
    '   $2 Dynamic = 02 头，aescrypt2，钥匙串内嵌在包头偏移 12（本身是 $2 字段）
    '   $4 Static  = 03 头，GCM，块头写 (1,2)，钥匙是 HwD4Reset 给的默认 D4Password
    '
    ' 其余不是「格式」而是「形态」：
    '   Boardinfo V5 / V6 = 24 字节头容器，必须显式选 —— 加密方向输入是明文，
    '                       明文里没有版本信息可跟，猜错就产出设备读不进的包
    '   Boardinfo $4 = 强制写 20 22 07 09 裸块
    '   Other $2 Encrypt = 01 头 + $2 静态钥匙，但 payload 不 gzip（customizepara.txt / *.tar.gz）
    '   Download $2 / $4 = 0712 下载包，内层分别是 02 / 03
    Private ReadOnly TargetNames As String() = {
        "Ctree $2 Static",
        "Ctree $2 Dynamic",
        "Ctree $4 Static",
        "Boardinfo V5",
        "Boardinfo V6",
        "Boardinfo $4",
        "Other $2 Encrypt",
        "Download $2 Static",
        "Download $2 Dynamic",
        "Download $4"
    }

    Private ReadOnly TargetCodes As String() = {"s1", "s2", "s4", "v5", "v6", "bin4", "raw1", "pkg1", "pkg", "pkg4"}

    ''' <summary>默认选中 CTREE $2 Dynamic，与命令行侧默认目标 s2 一致。</summary>
    Private Const DefaultTargetIndex As Integer = 1

    Private TargetIndex As Integer = DefaultTargetIndex

    ''' <summary>当前导入文件的识别格式。Boardinfo Vx 靠它决定导出版本，$4 Dynamic 靠它猜默认 Domain/Key。</summary>

    ''' <summary>填充原生 ComboBox：下拉箭头、下拉列表宽度都由系统负责，不再自绘。</summary>
    Private Sub FillTargetCombo()
        CmbTarget.BeginUpdate()
        CmbTarget.Items.Clear()
        For Each n As String In TargetNames
            CmbTarget.Items.Add(n)
        Next
        CmbTarget.EndUpdate()
        SelectTarget(DefaultTargetIndex)
    End Sub

    Private Sub SelectTarget(index As Integer)
        If index < 0 OrElse index >= TargetNames.Length Then index = DefaultTargetIndex
        TargetIndex = index
        CmbTarget.SelectedIndex = index
    End Sub

    Private Sub CmbTarget_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CmbTarget.SelectedIndexChanged
        If CmbTarget.SelectedIndex >= 0 Then TargetIndex = CmbTarget.SelectedIndex
    End Sub

    ' ---------- 密钥库定位 ----------

    ''' <summary>程序目录或上级目录里有 kmc_store_A/B 就自动认，没有就留空。</summary>
    Private Function FindLocalKeyStore() As String
        Try
            Dim dir As String = Path.GetDirectoryName(Application.ExecutablePath)
            Dim found As String = FindKeyStoreIn(dir)
            If found <> "" Then Return found
            Dim parent As String = Path.GetDirectoryName(dir)
            If Not String.IsNullOrEmpty(parent) Then Return FindKeyStoreIn(parent)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine("[忽略] 查找密钥库失败: " & ex.Message)
        End Try
        Return ""
    End Function

    Private Function FindKeyStoreIn(dir As String) As String
        If String.IsNullOrEmpty(dir) OrElse Not Directory.Exists(dir) Then Return ""
        For Each name As String In New String() {"kmc_store_A", "kmc_store_B", "kmc_store"}
            Dim p As String = Path.Combine(dir, name)
            If File.Exists(p) Then Return p
        Next
        Return ""
    End Function

    ''' <summary>拖入配置文件后，自动去同目录（再退一级）找配套的密钥库。</summary>
    Private Sub AutoLoadKeyStore(filePath As String)
        Try
            Dim dir As String = Path.GetDirectoryName(filePath)
            Dim found As String = FindKeyStoreIn(dir)
            If found = "" Then
                Dim parent As String = Path.GetDirectoryName(dir)
                If Not String.IsNullOrEmpty(parent) Then found = FindKeyStoreIn(parent)
            End If
            If found <> "" Then
                If TxtKmc.Text <> found Then
                    TxtKmc.Text = found
                    AppendLog("已载入 " & Path.GetFileName(found))
                End If
            Else
                ' 同目录没有就别留着上次别的设备的密钥库，容易解出乱码
                If TxtKmc.Text <> "" Then
                    AppendLog("未找到 kmc_store，已清空")
                    TxtKmc.Text = ""
                End If
            End If
        Catch ex As Exception
            AppendLog("探测密钥库时出错：" & ex.Message)
        End Try
    End Sub

    ' ---------- 拖放 ----------

    ''' <summary>窗体本身也接拖放：不必非得瞄准那根细输入框。</summary>
    Private Sub MiniForm_DragEnter(sender As Object, e As DragEventArgs) Handles MyBase.DragEnter
        e.Effect = If(HasFiles(e), DragDropEffects.Copy, DragDropEffects.None)
    End Sub

    Private Sub MiniForm_DragDrop(sender As Object, e As DragEventArgs) Handles MyBase.DragDrop
        Dim paths As String() = GetDroppedFiles(e)
        If paths Is Nothing Then Return
        LoadConfigPaths(paths)
    End Sub

    Private Sub TxtCfgFile_DragEnter(sender As Object, e As DragEventArgs) Handles TxtCfgFile.DragEnter
        e.Effect = If(HasFiles(e), DragDropEffects.Copy, DragDropEffects.None)
    End Sub

    Private Sub TxtCfgFile_DragDrop(sender As Object, e As DragEventArgs) Handles TxtCfgFile.DragDrop
        Dim paths As String() = GetDroppedFiles(e)
        If paths Is Nothing Then Return
        LoadConfigPaths(paths)
    End Sub

    Private Sub TxtKmc_DragEnter(sender As Object, e As DragEventArgs) Handles TxtKmc.DragEnter
        e.Effect = If(HasFiles(e), DragDropEffects.Copy, DragDropEffects.None)
    End Sub

    Private Sub TxtKmc_DragDrop(sender As Object, e As DragEventArgs) Handles TxtKmc.DragDrop
        Dim paths As String() = GetDroppedFiles(e)
        If paths Is Nothing OrElse paths.Length = 0 Then Return
        If File.Exists(paths(0)) Then
            TxtKmc.Text = paths(0)
            AppendLog("已指定 " & Path.GetFileName(paths(0)))
        End If
    End Sub

    Private Sub TxtFieldIn_DragEnter(sender As Object, e As DragEventArgs) Handles TxtFieldIn.DragEnter
        e.Effect = If(HasFiles(e), DragDropEffects.Copy, DragDropEffects.None)
    End Sub

    ''' <summary>把文件拖进字段框时，读文件内容当输入（boardinfo 里常是一整条 $2 串）。</summary>
    Private Sub TxtFieldIn_DragDrop(sender As Object, e As DragEventArgs) Handles TxtFieldIn.DragDrop
        Dim paths As String() = GetDroppedFiles(e)
        If paths Is Nothing OrElse paths.Length = 0 Then Return
        Try
            Dim txt As String = File.ReadAllText(paths(0)).Trim()
            TxtFieldIn.Text = txt
            AppendLog("读入 " & txt.Length & " 字符")
        Catch ex As Exception
            AppendLog("读取失败：" & ex.Message)
        End Try
    End Sub

    Private Function HasFiles(e As DragEventArgs) As Boolean
        Return e.Data IsNot Nothing AndAlso e.Data.GetDataPresent(DataFormats.FileDrop)
    End Function

    Private Function GetDroppedFiles(e As DragEventArgs) As String()
        If Not HasFiles(e) Then Return Nothing
        Try
            Return TryCast(e.Data.GetData(DataFormats.FileDrop), String())
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>把拖入的路径收进列表：目录展开成文件，显示成一行摘要。</summary>
    Private Sub LoadConfigPaths(paths As String())
        If paths Is Nothing OrElse paths.Length = 0 Then Return

        CfgFiles.Clear()
        For Each p As String In paths
            If Directory.Exists(p) Then
                Try
                    CfgFiles.AddRange(Directory.GetFiles(p))
                Catch ex As Exception
                    AppendLog("跳过目录 " & p & "：" & ex.Message)
                End Try
            ElseIf File.Exists(p) Then
                CfgFiles.Add(p)
            End If
        Next
        If CfgFiles.Count = 0 Then Return

        If CfgFiles.Count = 1 Then
            TxtCfgFile.Text = CfgFiles(0)
        Else
            TxtCfgFile.Text = String.Format("共 {0} 个文件，{1} 等", CfgFiles.Count, Path.GetFileName(CfgFiles(0)))
        End If

        AutoLoadKeyStore(CfgFiles(0))
        UpdateTypeTag(CfgFiles.Count)
    End Sub

    ' ---------- 文件类型标签 ----------

    ''' <summary>
    ''' 标题行右边那个胶囊显示「导进来的到底是什么」，名字由 HwContainer.KindName 给。
    ''' 那套名字回答「这个文件是什么」；下拉框回答「要加密成什么」，两个维度别混。
    ''' KindName 要读全文（01 容器得解一层才知道压没压），所以这里不是只读文件头。
    ''' </summary>
    Private Sub UpdateTypeTag(count As Integer)
        If count = 0 Then
            LblTypeTag.Text = "未导入"
            Return
        End If
        If count > 1 Then
            LblTypeTag.Text = count & " 个文件"
            Return
        End If
        Try
            Dim buf As Byte() = ReadForTag(CfgFiles(0))
            If buf.Length < 4 Then
                LblTypeTag.Text = "文件过小"
                Return
            End If
            LblTypeTag.Text = HwContainer.KindName(buf, CfgFiles(0))
        Catch
            LblTypeTag.Text = "无法识别"
        End Try
    End Sub

    ''' <summary>读文件给标签用。FileShare.ReadWrite 是必须的 —— 文件可能正被别的程序开着。</summary>
    Private Shared Function ReadForTag(path As String) As Byte()
        Const MaxLen As Long = 64L * 1024 * 1024
        Using fs As New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Dim len As Integer = CInt(Math.Min(fs.Length, MaxLen))
            Dim buf(len - 1) As Byte
            Dim read As Integer = 0
            While read < len
                Dim k As Integer = fs.Read(buf, read, len - read)
                If k <= 0 Then Exit While
                read += k
            End While
            If read < len Then Array.Resize(buf, read)
            Return buf
        End Using
    End Function

    ''' <summary>胶囊只有 150px，用短名；长名（如 "07 12 21 20 下载包"）会被截。</summary>
    Private Function ShortFormatName(f As HwFileFormat) As String
        Select Case f
            Case HwFileFormat.PlainText : Return "明文"
            Case HwFileFormat.CtreeStatic : Return "01 静态容器"
            Case HwFileFormat.CtreeDynamic : Return "02 动态容器"
            Case HwFileFormat.CtreeD4 : Return "03 $4 容器"
            Case HwFileFormat.D4Binary : Return "boardinfo 裸块"
            Case HwFileFormat.DownloadPackage : Return "下载配置包"
            Case HwFileFormat.BoardInfoV5 : Return "boardinfo V5"
            Case HwFileFormat.BoardInfoV6 : Return "boardinfo V6"
            Case HwFileFormat.Ctce8Cfg : Return "CFG 包"
            Case HwFileFormat.KsfV2 : Return "密钥库"
            Case HwFileFormat.GzipStream : Return "GZip 流"
            Case Else : Return "无法识别"
        End Select
    End Function

    ' ---------- 输出框 ----------

    Private Sub ShowOutput(text As String)
        If String.IsNullOrEmpty(text) Then
            TxtFieldOut.ForeColor = OutPlaceColor
            TxtFieldOut.Text = OutPlaceholder
        Else
            TxtFieldOut.ForeColor = OutNormalColor
            TxtFieldOut.Text = text
        End If
    End Sub

    Private Function OutputValue() As String
        If TxtFieldOut.ForeColor = OutPlaceColor Then Return ""
        Return TxtFieldOut.Text
    End Function

    ''' <summary>
    ''' 底部提示行：只放最近一条消息，单行，只读但可选可复制。
    ''' 不做滚动历史 —— 要翻日志就去看弹窗那一次性的汇总，这里只回答「刚才发生了什么」。
    ''' </summary>
    Private Sub SetMsg(text As String)
        TxtMsg.Text = If(text, "").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
        ' 光标停回开头，否则长文本会停在尾部只露出一截
        TxtMsg.SelectionStart = 0
        TxtMsg.SelectionLength = 0
    End Sub

    ' ---------- 按钮 ----------

    Private Sub BtnCfgBrowse_Click(sender As Object, e As EventArgs) Handles BtnCfgBrowse.Click
        If Busy Then Return
        Using dlg As New OpenFileDialog()
            dlg.Title = "选择配置文件"
            dlg.Multiselect = True
            dlg.Filter = "配置文件|hw_ctree*;hw_boardinfo*;*.xml;*.bin;*.dat|所有文件|*.*"
            If dlg.ShowDialog(Me) = DialogResult.OK Then LoadConfigPaths(dlg.FileNames)
        End Using
    End Sub

    Private Sub BtnKmcBrowse_Click(sender As Object, e As EventArgs) Handles BtnKmcBrowse.Click
        If Busy Then Return
        Using dlg As New OpenFileDialog()
            dlg.Title = "选择密钥库"
            dlg.Filter = "密钥库|kmc_store*|所有文件|*.*"
            If dlg.ShowDialog(Me) = DialogResult.OK Then TxtKmc.Text = dlg.FileName
        End Using
    End Sub

    Private Sub BtnDecrypt_Click(sender As Object, e As EventArgs) Handles BtnDecrypt.Click
        RunOnConfig("dec")
    End Sub

    Private Sub BtnEncrypt_Click(sender As Object, e As EventArgs) Handles BtnEncrypt.Click
        RunOnConfig("enc")
    End Sub

    ' ---------- 配置文件处理 ----------

    Private Sub RunOnConfig(cmd As String)
        If Busy Then Return
        If CfgFiles.Count = 0 Then
            MessageBox.Show(Me, "请先选择配置文件", "HW Dollar4 mini",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Program.Opt.Target = TargetCodes(TargetIndex)
        Program.Opt.KmcPath = TxtKmc.Text.Trim()
        Program.Opt.Passphrase = TxtPass.Text
        Program.Opt.HasPassphrase = (TxtPass.Text <> "")
        ' 备份恒定开启：原文件改名 001_原名 是唯一的后悔药，不提供关闭入口
        Program.Opt.NoBackup = False

        Dim op As String = If(cmd = "dec", "解密", "加密")
        SetBusy(True)
        Application.DoEvents()

        Dim rc As Integer = 1
        Program.LogSink = AddressOf AppendLog
        Try
            rc = Program.Execute(CfgFiles, cmd)
        Catch ex As Exception
            AppendLog("错误：" & ex.Message)
            rc = 1
        Finally
            Program.LogSink = Nothing
            SetBusy(False)
        End Try

        If rc <> 0 Then ShowCollectedLog(op & "有文件未成功")

        ' 文件是原地改写的（旧内容改名成 001_原名，新内容写回原路径）。
        ' 所以处理完必须重读一次，否则右上角胶囊还停在导入时的识别结果 ——
        ' 解密完仍显示「Other $2 Encrypt」、加密完仍显示「Other Decrypt」。
        UpdateTypeTag(CfgFiles.Count)
    End Sub

    ' ---------- 密文字段处理 ----------

    Private Sub BtnFieldAuto_Click(sender As Object, e As EventArgs) Handles BtnFieldAuto.Click
        If Busy Then Return
        Dim src As String = TxtFieldIn.Text.Trim()
        If src = "" Then
            Warn("请先输入字段")
            Return
        End If
        If Not src.StartsWith("$") Then
            ShowOutput(src)
            AppendLog("非 $ 字段，原样输出")
            Return
        End If

        SetBusy(True)
        Try
            PrepareKeys()
            Dim head As String = src.Substring(1, 1)
            Dim plain As String = HwContainer.DecryptFieldText(src)
            If plain = src Then
                ShowOutput("")
                Warn("解密失败，密钥不对")
            Else
                ShowOutput(plain.TrimEnd(ChrW(0)))
                AppendLog("$" & head & " → " & TxtFieldOut.Text.Length & " 字符")
            End If
        Catch ex As Exception
            Warn("解密出错：" & ex.Message)
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Sub BtnFieldD2B_Click(sender As Object, e As EventArgs) Handles BtnFieldD2B.Click
        DecryptD2As("BOARDINFO")
    End Sub

    Private Sub BtnFieldD2O_Click(sender As Object, e As EventArgs) Handles BtnFieldD2O.Click
        DecryptD2As("OMCI")
    End Sub

    ''' <summary>$2 的三套盐只差一个后缀常量，靠 HwCtx.D2KeyType 切换。</summary>
    Private Sub DecryptD2As(keyType As String)
        If Busy Then Return
        Dim src As String = TxtFieldIn.Text.Trim()
        If src = "" Then
            Warn("请先输入字段")
            Return
        End If
        If Not src.StartsWith("$2") Then
            AppendLog("非 $2，按 " & keyType & " 试")
        End If

        SetBusy(True)
        Try
            PrepareKeys()
            HwCtx.D2KeyType = keyType
            Dim plain As String = HwField.HW_D2Decrypt(src)
            If plain Is Nothing OrElse plain.Length = 0 Then
                ShowOutput("")
                Warn("结果为空，密钥不匹配")
            Else
                ShowOutput(plain.TrimEnd(ChrW(0)))
                AppendLog("$2 " & keyType & " → " & TxtFieldOut.Text.Length & " 字符")
            End If
        Catch ex As Exception
            Warn("$2 " & keyType & " 出错：" & ex.Message)
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Sub BtnFieldE1_Click(sender As Object, e As EventArgs) Handles BtnFieldE1.Click
        EncryptField("s1")
    End Sub

    Private Sub BtnFieldE2_Click(sender As Object, e As EventArgs) Handles BtnFieldE2.Click
        EncryptField("s2")
    End Sub

    Private Sub BtnFieldE4_Click(sender As Object, e As EventArgs) Handles BtnFieldE4.Click
        EncryptField("s4")
    End Sub

    Private Sub EncryptField(kind As String)
        If Busy Then Return
        Dim src As String = TxtFieldIn.Text
        If src Is Nothing OrElse src.Length = 0 Then
            Warn("请先输入字段")
            Return
        End If
        ' 已经是密文就别再套一层，免得越滚越长
        If src.TrimStart().StartsWith("$") Then
            Warn("已是密文，请先解密")
            Return
        End If

        SetBusy(True)
        Try
            PrepareKeys()
            Dim r As String
            Select Case kind
                Case "s1" : r = HwField.HW_D1Encrypt(src)
                Case "s4" : r = HwField.HW_D4EncryptString(src)
                Case Else : r = HwField.HW_D2Encrypt(src)
            End Select
            If r Is Nothing OrElse r.Length = 0 Then
                Warn("加密失败，缺少 $4 主密钥")
            Else
                ShowOutput(r)
                AppendLog("$" & kind.Substring(1, 1) & " 加密 → " & r.Length & " 字符")
            End If
        Catch ex As Exception
            Warn("加密出错：" & ex.Message)
        Finally
            SetBusy(False)
        End Try
    End Sub

    Private Sub BtnCopyField_Click(sender As Object, e As EventArgs) Handles BtnCopyField.Click
        Dim outText As String = OutputValue()
        If outText = "" Then
            Warn("输出为空")
            Return
        End If
        Try
            Clipboard.SetText(outText)
            AppendLog("已复制 " & outText.Length & " 字符")
        Catch ex As Exception
            Warn("复制失败：" & ex.Message)
        End Try
    End Sub

    ''' <summary>把界面上的密钥设置同步给内核，再让它加载密钥库。</summary>
    Private Sub PrepareKeys()
        Program.Opt.KmcPath = TxtKmc.Text.Trim()
        Program.Opt.D2KeyType = "OS"
        Program.Opt.Passphrase = TxtPass.Text
        Program.Opt.HasPassphrase = (TxtPass.Text <> "")
        Program.PrepareKeys()
    End Sub

    ''' <summary>内核在密钥不够时回调这里。返回 True 表示用户给了密码。</summary>
    Private Function AskPassword(dir As String) As Boolean
        Dim hint As String = "输入 32 字节 HEX 主密钥"
        If dir <> "" Then hint = "密钥库无匹配项，请手输主密钥" & vbCrLf & "密钥库：" & dir
        Dim s As String = Microsoft.VisualBasic.Interaction.InputBox(hint, "需要主密钥", "")
        If s Is Nothing OrElse s.Trim() = "" Then Return False
        s = s.Replace(" ", "").Replace("-", "")
        Dim b As Byte() = HwBytes.HexToBytes(s)
        If b.Length = 0 Then
            Warn("密钥不是合法 HEX")
            Return False
        End If
        HwCtx.D4Password = b
        Return True
    End Function

    ' ---------- 状态 ----------

    Private Sub SetBusy(busy As Boolean)
        Busy = busy
        For Each c As Control In New Control() {TxtCfgFile, BtnCfgBrowse, TxtKmc, BtnKmcBrowse, CmbTarget,
                                                BtnDecrypt, BtnEncrypt, TxtPass,
                                                TxtFieldIn, TxtFieldOut, BtnCopyField,
                                                BtnFieldAuto, BtnFieldD2B, BtnFieldD2O,
                                                BtnFieldE1, BtnFieldE2, BtnFieldE4}
            c.Enabled = Not busy
        Next
        Cursor = If(busy, Cursors.WaitCursor, Cursors.Default)
    End Sub

    ''' <summary>
    ''' 界面上没有日志区，消息只进内存 + 底部那一行提示。
    ''' 正常流程每一步都过这里，所以提示行永远显示最近一步的结果；出问题另有弹窗兜底。
    ''' </summary>
    Private ReadOnly LogLines As New List(Of String)

    Private Sub AppendLog(msg As String)
        Dim one As String = If(msg, "").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
        If one = "" Then Return
        LogLines.Add(one)
        If LogLines.Count > 200 Then LogLines.RemoveAt(0)
        SetMsg(one)
    End Sub

    ''' <summary>失败提示：必须让用户看见，所以弹窗。</summary>
    Private Sub Warn(msg As String)
        AppendLog(msg)
        MessageBox.Show(Me, msg, "HW Dollar4 mini", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    ''' <summary>一批文件跑完后有失败，把这次收集到的日志一次性摊开。</summary>
    Private Sub ShowCollectedLog(title As String)
        If LogLines.Count = 0 Then Return
        Dim body As String = String.Join(vbCrLf, LogLines.ToArray())
        If body.Length > 2000 Then body = "…" & body.Substring(body.Length - 2000)
        MessageBox.Show(Me, title & vbCrLf & vbCrLf & body, "HW Dollar4 mini", MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

End Class
