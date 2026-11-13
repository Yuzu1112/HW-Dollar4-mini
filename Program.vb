' HW Dollar4 mini —— 单 exe 双模式：双击开界面，带参数走命令行。
'
' 落盘规则（固定行为，不可配置）：
'   原文件改名为 001_原名（重名自动 002、003…），转换结果写回原文件名。
'   即「路径不变、旧版本全留」，连续处理 10 次会得到 001_..010_ 十个历史版本。
'
' 自动模式：不写子命令时，密文自动解、明文自动加密（默认 02 动态容器，不需要密钥）。
'
' 两种入口共用同一套执行代码（Execute），GUI 只是把日志换个去处。
Imports System.Runtime.InteropServices
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Public Module Program

    Friend Class CliOptions
        Public KeyHex As String = ""
        Public KmcPath As String = ""
        Public Passphrase As String = ""
        Public HasPassphrase As Boolean = False
        Public Target As String = ""
        Public D2KeyType As String = "OS"
        Public NoBackup As Boolean = False
        Public Quiet As Boolean = False
        Public Force As Boolean = False
        ''' <summary>加密下载包时写进偏移 4 的口令模式：1 = 口令直用，2 = 先派生。默认 2。</summary>
        Public PolicyMode As Integer = 2
        ''' <summary>加密下载包时写进偏移 12 的策略盐（明文）。空串则随机生成。</summary>
        Public PolicySaltText As String = ""
    End Class

    Friend ReadOnly Opt As New CliOptions

    ''' <summary>拖放/双击启动时为 True：可以停下来等人敲东西（命令行脚本里绝不能阻塞）。</summary>
    Private Interactive As Boolean = False

    ''' <summary>日志出口。Nothing 时写控制台；GUI 会换成往 TextBox 追加。</summary>
    Public LogSink As Action(Of String) = Nothing

    ''' <summary>GUI 模式：有 LogSink 就是往界面回灌，提示可以用界面语言而非命令行参数。</summary>
    Friend ReadOnly Property GuiMode As Boolean
        Get
            Return LogSink IsNot Nothing
        End Get
    End Property

    Friend Sub Emit(msg As String)
        If LogSink IsNot Nothing Then
            LogSink(msg)
        Else
            Console.WriteLine(msg)
        End If
    End Sub

    Friend Sub EmitError(msg As String)
        If LogSink IsNot Nothing Then
            LogSink("错误：" & msg)
        Else
            Console.Error.WriteLine(msg)
        End If
    End Sub

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function AttachConsole(dwProcessId As Integer) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function AllocConsole() As Boolean
    End Function

    ''' <summary>
    ''' WinExe 默认不挂控制台，从 cmd 调用时输出会丢。
    ''' 先尝试挂到父进程的控制台；没有父控制台（双击启动）才自己开一个。
    ''' </summary>
    Private Sub EnsureConsole()
        Const ATTACH_PARENT_PROCESS As Integer = -1
        If Not AttachConsole(ATTACH_PARENT_PROCESS) Then
            If Not AllocConsole() Then Return
        End If
        Try
            Dim so As New StreamWriter(Console.OpenStandardOutput()) With {.AutoFlush = True}
            Dim se As New StreamWriter(Console.OpenStandardError()) With {.AutoFlush = True}
            Console.SetOut(so)
            Console.SetError(se)
        Catch
        End Try
    End Sub

    ''' <summary>把界面异常写进 exe 同目录的日志，方便定位；写不进去也不能再抛。</summary>
    Private Sub DumpGuiError(ex As Exception)
        Try
            Dim exeDir As String = Path.GetDirectoryName(GetType(Program).Assembly.Location)
            If String.IsNullOrEmpty(exeDir) Then exeDir = "."
            File.WriteAllText(Path.Combine(exeDir, "mini_gui_error.log"),
                              DateTime.Now.ToString("s") & vbCrLf & ex.ToString(), Encoding.UTF8)
        Catch
        End Try
    End Sub

    Private Sub OnGuiThreadException(sender As Object, e As System.Threading.ThreadExceptionEventArgs)
        DumpGuiError(e.Exception)
        System.Windows.Forms.MessageBox.Show(e.Exception.ToString(), "HW Dollar4 mini 运行错误")
    End Sub

    Private Sub OnDomainException(sender As Object, e As UnhandledExceptionEventArgs)
        DumpGuiError(TryCast(e.ExceptionObject, Exception))
    End Sub

    Public Function Main(args As String()) As Integer
        ' 不带任何参数 = 双击启动，开界面
        If args Is Nothing OrElse args.Length = 0 Then
            Try
                System.Windows.Forms.Application.EnableVisualStyles()
                System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(False)
                ' 界面里的异常会被 WinForms 自己吞掉，Main 的 catch 根本看不到，
                ' 必须挂在这两个地方才拿得到堆栈。
                AddHandler System.Windows.Forms.Application.ThreadException, AddressOf OnGuiThreadException
                AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainException
                System.Windows.Forms.Application.Run(New MiniForm())
                Return 0
            Catch ex As Exception
                DumpGuiError(ex)
                System.Windows.Forms.MessageBox.Show(ex.ToString(), "HW Dollar4 mini 启动失败")
                Return 1
            End Try
        End If

        EnsureConsole()
        Try
            Console.OutputEncoding = Encoding.UTF8
        Catch
        End Try
        Try
            Return Run(args)
        Catch ex As Exception
            EmitError(ex.Message)
            Return 1
        End Try
    End Function

    Private Function Run(args As String()) As Integer
        Dim files As New List(Of String)
        Dim cmd As String = ""
        Dim i As Integer = 0

        While i < args.Length
            Dim a As String = args(i)
            Dim al As String = a.ToLowerInvariant()
            Select Case al
                Case "dec", "enc", "field", "kmc", "info"
                    cmd = al
                Case "--key"
                    i += 1 : RequireArg(args, i, "--key")
                    Opt.KeyHex = args(i)
                Case "--kmc"
                    i += 1 : RequireArg(args, i, "--kmc")
                    Opt.KmcPath = args(i)
                Case "--pass", "--password", "--passphrase"
                    i += 1 : RequireArg(args, i, "--pass")
                    Opt.Passphrase = args(i)
                    Opt.HasPassphrase = True
                Case "--to"
                    i += 1 : RequireArg(args, i, "--to")
                    Opt.Target = args(i).ToLowerInvariant()
                Case "--d2key"
                    i += 1 : RequireArg(args, i, "--d2key")
                    Opt.D2KeyType = args(i)
                Case "--pmode"
                    i += 1 : RequireArg(args, i, "--pmode")
                    Dim pm As Integer
                    If Not Integer.TryParse(args(i), pm) OrElse (pm <> 1 AndAlso pm <> 2) Then
                        Throw New ArgumentException("--pmode 只能是 1（口令直用）或 2（先派生）")
                    End If
                    Opt.PolicyMode = pm
                Case "--psalt"
                    i += 1 : RequireArg(args, i, "--psalt")
                    Opt.PolicySaltText = args(i)
                Case "-y", "--overwrite"
                    Opt.NoBackup = True
                Case "-f", "--force"
                    Opt.Force = True
                Case "-q", "--quiet"
                    Opt.Quiet = True
                Case "-h", "--help", "/?", "/help"
                    ShowHelp()
                    Return 0
                Case Else
                    If a.StartsWith("--") OrElse a.StartsWith("-") Then Throw New ArgumentException("未知选项：" & a)
                    files.Add(a)
            End Select
            i += 1
        End While

        ' 拖放场景：参数里只有文件，没有子命令也没有选项 —— 处理完停住，好让人看见结果
        Dim dragDrop As Boolean = (cmd = "" AndAlso files.Count > 0 AndAlso files.Count = args.Length)
        Interactive = dragDrop

        Dim rc As Integer = Execute(files, cmd)

        If dragDrop Then
            Try
                Console.WriteLine()
                Console.WriteLine("处理完毕，按任意键退出...")
                Console.ReadKey(True)
            Catch
            End Try
        End If
        Return rc
    End Function

    ''' <summary>真正的执行入口，GUI 和 CLI 共用。调用前把 Opt 填好即可。</summary>
    Friend Function Execute(files As List(Of String), cmd As String) As Integer
        InitKeys()

        If cmd = "kmc" Then Return CmdKmc(files)
        If cmd = "field" Then Return CmdField(files)
        If files Is Nothing OrElse files.Count = 0 Then
            ShowHelp()
            Return 2
        End If
        If cmd = "info" Then Return CmdInfo(files)

        Dim rc As Integer = 0
        For Each f As String In files
            If Not File.Exists(f) Then
                EmitError("文件不存在：" & f)
                rc = 1
                Continue For
            End If
            If ProcessFile(f, cmd) <> 0 Then rc = 1
        Next
        Return rc
    End Function

    Private Sub RequireArg(args As String(), index As Integer, name As String)
        If index >= args.Length Then Throw New ArgumentException(name & " 缺少参数")
    End Sub

    ' ---------- 密钥准备 ----------

    ''' <summary>给 GUI 用：按当前 Opt 重新加载密钥库与口令。字段区每次操作前都要调一次。</summary>
    Friend Sub PrepareKeys()
        InitKeys()
    End Sub

    Private Sub InitKeys()
        HwCtx.D2KeyType = Opt.D2KeyType
        HwCtx.D4KeyStoreLast = HwKmc.AppDir()

        ' 顺序不能变：HwD4Reset 会清空 D4KeyStoreFile，必须在加载密钥库之前调用
        HwCtx.HwD4Reset()

        If Opt.KmcPath <> "" Then
            If Directory.Exists(Opt.KmcPath) Then
                HwKmc.LoadKeyStore(Opt.KmcPath)
            ElseIf File.Exists(Opt.KmcPath) Then
                HwCtx.D4KeyStoreFile = File.ReadAllBytes(Opt.KmcPath)
            Else
                Throw New FileNotFoundException("找不到密钥库：" & Opt.KmcPath)
            End If
        Else
            HwKmc.LoadKeyStore()
        End If

        If Opt.KeyHex <> "" Then
            Dim hex As String = Opt.KeyHex.Replace(" ", "").Replace("-", "")
            HwCtx.D4Password = HwBytes.HexToBytes(hex)
            If HwCtx.D4Password.Length = 0 Then Throw New ArgumentException("--key 不是合法的 HEX 字符串")
        End If

        ' 人工口令。设备出厂时被人工设过密码的下载包，只有这条路能解，
        ' 跟 kmc_store 里的主密钥完全是两回事。
        HwCtx.PolicyPassphrase = If(Opt.HasPassphrase, Opt.Passphrase, "")
    End Sub

    ' ---------- 单文件处理 ----------

    ''' <summary>
    ''' 加密下载包前的口令策略准备。只有「内层 03 + 给了 --pass」才算人工口令包；
    ''' 其余情况一律把模式清零 —— 解密时 UnwrapDownload 会把包头的 PolicyMode/PolicySalt
    ''' 填进同一份全局状态，不清就会把上一个包的口令策略带进新包里。
    ''' </summary>
    ''' <summary>
    ''' 目标是「导出明文」那三项时为 True。这几项目标输入本来就是密文，要放行而不是拦下。
    ''' 注意不含 "raw" —— 那是旧的「原样写回明文」语义，行为不能变。
    ''' </summary>
    Private Function IsPlainExportTarget() As Boolean
        Select Case If(Opt.Target, "").ToLowerInvariant()
            Case "text", "btext", "otext", "p1" : Return True
            Case Else : Return False
        End Select
    End Function

    Private Sub PrepPolicyForEncrypt(inner As HwFileFormat)
        HwCtx.PolicyMode = 0
        HwCtx.PolicySalt = New Byte() {}

        If inner <> HwFileFormat.CtreeD4 Then Return
        If Not Opt.HasPassphrase OrElse Opt.Passphrase = "" Then Return

        HwCtx.PolicyMode = Opt.PolicyMode
        If Opt.PolicyMode = 2 Then
            If Opt.PolicySaltText <> "" Then
                If Opt.PolicySaltText.Length > HwCtx.PolicySaltMax Then
                    Throw New ArgumentException("--psalt 最长 " & HwCtx.PolicySaltMax & " 字节")
                End If
                HwCtx.PolicySalt = Encoding.UTF8.GetBytes(Opt.PolicySaltText)
            Else
                HwCtx.PolicySalt = HwCtx.MakePolicySalt()
            End If
        End If

        ' 人工口令包的内层 03 块写 0/0、包头 96/100 也写 0/0 —— 跟真实样本一致：
        ' 走口令这条路就与设备密钥库无关，留 Domain/Key 只会误导解包方去查库。
        HwCtx.D4KeyStore = New Byte(3) {}
        HwCtx.D4Password = HwCtx.DerivePolicyPassword()
        If HwCtx.D4Password.Length = 0 Then Throw New InvalidDataException("口令派生结果为空")
    End Sub

    ''' <summary>打包说明里附加口令策略，让人一眼看出这个包要不要密码。</summary>
    Private Function PolicyNote() As String
        If HwCtx.PolicyMode = 0 Then Return ""
        Dim s As String = " 模式" & HwCtx.PolicyMode
        If HwCtx.PolicyMode = 2 Then s &= " 盐" & Encoding.UTF8.GetString(HwCtx.PolicySalt)
        Return s
    End Function

    Private Function ProcessFile(filePath As String, cmd As String) As Integer
        Dim buf As Byte() = File.ReadAllBytes(filePath)
        Dim fmt As HwFileFormat = HwContainer.DetectFormat(buf)

        Dim wantDecrypt As Boolean
        If cmd = "dec" Then
            wantDecrypt = True
        ElseIf cmd = "enc" Then
            wantDecrypt = False
        Else
            wantDecrypt = (fmt <> HwFileFormat.PlainText AndAlso fmt <> HwFileFormat.Unknown)
        End If

        Dim outBytes As Byte() = Nothing
        Dim outFmt As HwFileFormat = HwFileFormat.PlainText
        Dim note As String
        Dim needHash As Boolean = (fmt = HwFileFormat.BoardInfoV5 OrElse fmt = HwFileFormat.BoardInfoV6)

        If wantDecrypt Then
            If fmt = HwFileFormat.PlainText Then
                Emit(Path.GetFileName(filePath) & "：已是明文")
                Return 0
            End If
            Dim r As HwOpenResult = HwContainer.OpenConfig(buf)

            ' 下载包内层若是人工设过密码的 $4，密钥库里根本没有这把钥匙，只能问人。
            ' 命令行漏了 --pass 时提示补；拖放场景直接读一行，省得重开命令行。
            If Not r.Success AndAlso fmt = HwFileFormat.DownloadPackage AndAlso Not Opt.HasPassphrase Then
                If Interactive Then
                    Console.Write(Path.GetFileName(filePath) & "：这个下载包需要人工口令，请输入密码（直接回车跳过）：")
                    Dim typed As String = ""
                    Try
                        typed = Console.ReadLine()
                    Catch
                    End Try
                    If Not String.IsNullOrEmpty(typed) Then
                        Opt.Passphrase = typed
                        Opt.HasPassphrase = True
                        HwCtx.PolicyPassphrase = typed
                        r = HwContainer.OpenConfig(buf)
                    End If
                End If
            End If

            If Not r.Success Then
                EmitError(Path.GetFileName(filePath) & "：解密失败：" & r.ErrorMessage)
                If fmt = HwFileFormat.DownloadPackage AndAlso Not Opt.HasPassphrase Then
                    EmitError(If(GuiMode, "  下载包需要口令，请填「下载密钥」", "  该下载包内层多半是人工口令加密的 $4，请用 --pass <密码> 再试"))
                End If
                Return 1
            End If
            outBytes = New UTF8Encoding(False).GetBytes(r.Text)
            note = String.Format("{0} → 明文 {1} 字节", HwContainer.FormatName(r.SourceFormat), outBytes.Length)
        Else
            If fmt <> HwFileFormat.PlainText AndAlso cmd = "" Then
                Emit(Path.GetFileName(filePath) & "：已是密文")
                Return 0
            End If
            Dim inner As HwFileFormat = HwFileFormat.CtreeDynamic
            Dim target As HwFileFormat = ParseTarget(Opt.Target, inner)
            ' 「Ctree Decrypt / Boardinfo Decrypt / Other Decrypt」是解包方向：输入本来就是密文，
            ' 既不能被「二次加密」拦下，也不能拿明文去 BuildConfig —— 走 BuildPlain。
            Dim toPlain As Boolean = IsPlainExportTarget()

            ' 强制加密时挡住「密文再加密一层」这种误操作，套娃结果没法还原
            If Not toPlain AndAlso fmt <> HwFileFormat.PlainText AndAlso Not Opt.Force Then
                EmitError(Path.GetFileName(filePath) & "：已是密文 " & HwContainer.FormatName(fmt) & If(GuiMode, "，拒绝二次加密", "，拒绝二次加密；确需如此请加 -f"))
                Return 1
            End If
            Dim text As String = HwContainer.StripBom(Encoding.UTF8.GetString(buf))

            If toPlain Then
                outBytes = HwContainer.BuildPlain(buf)
                outFmt = HwFileFormat.PlainText
                note = String.Format("{0} → 明文 {1} 字节", HwContainer.FormatName(fmt), outBytes.Length)
            ElseIf target = HwFileFormat.DownloadPackage Then
                PrepPolicyForEncrypt(inner)
                Dim innerBytes As Byte() = BuildConfigText(text, inner)
                outBytes = HwContainer.BuildDownloadPackage(innerBytes, inner)
                outFmt = inner
                note = String.Format("明文 → 下载包 内层 {0}{1} {2} 字节", HwContainer.FormatName(inner), If(GuiMode, "", PolicyNote()), outBytes.Length)
            Else
                outBytes = BuildConfigText(text, target)
                outFmt = target
                note = String.Format("明文 → {0} {1} 字节", HwContainer.FormatName(target), outBytes.Length)
                If target = HwFileFormat.CtreeD4 OrElse target = HwFileFormat.D4Binary Then
                    note &= "（" & HwD4Enc.KeySource() & "）"
                End If
            End If

            If outBytes Is Nothing OrElse outBytes.Length = 0 Then
                EmitError(Path.GetFileName(filePath) & "：加密失败" & If(GuiMode, "，缺少 $4 主密钥", "，缺少 $4 主密钥，请用 --key 或指定密钥库"))
                Return 1
            End If
            needHash = needHash OrElse (outFmt = HwFileFormat.BoardInfoV5 OrElse outFmt = HwFileFormat.BoardInfoV6)
        End If

        WriteWithBackup(filePath, outBytes)
        If needHash Then WriteHashFile(filePath, outBytes)
        If Not Opt.Quiet Then Emit(Path.GetFileName(filePath) & "：" & note)
        Return 0
    End Function

    ' ---------- 加密：$4 走独立模块 ----------

    ''' <summary>
    ''' 按目标格式产出密文。$4 的两个目标交给 <see cref="HwD4Enc"/>，其余仍用共享内核。
    ''' 分开的原因：共享内核的 $4 加密读的是全局口令槽，槽被清空就直接失败；
    ''' HwD4Enc 会回落到出厂的 (1,2) + $2 密钥，所以 $4 加密不依赖「之前有没有成功解过文件」。
    ''' 下载包的内层若是 $4，也走同一条路，保证内层 03 块与单独导出的完全同构。
    ''' </summary>
    Private Function BuildConfigText(text As String, target As HwFileFormat) As Byte()
        Select Case target
            Case HwFileFormat.CtreeD4
                Return HwD4Enc.BuildCtree(text, HwD4Enc.ResolveKey(), HwD4Enc.ResolveDomainKey())
            Case HwFileFormat.D4Binary
                Return HwD4Enc.BuildBinaryText(text, HwD4Enc.ResolveKey(), HwD4Enc.ResolveDomainKey())
            Case HwFileFormat.BoardInfoV5, HwFileFormat.BoardInfoV6
                ' 与上游 HW Dollar4 一致：20 字节头后面的负载是 $2 令牌，不是明文。
                ' 输入本身已是 $2/$4 令牌就原样使用，避免在真机样本上再套一层。
                Dim t As String = text.Trim()
                Dim payload As String = text
                If Not ((t.StartsWith("$2") OrElse t.StartsWith("$4")) AndAlso t.EndsWith("$")) Then
                    payload = HwField.HW_D2Encrypt(text)
                End If
                Return HwContainer.BuildConfig(payload, target)
            Case Else
                Return HwContainer.BuildConfig(text, target)
        End Select
    End Function

    ' ---------- 落盘 ----------

    ''' <summary>原文件改名 001_原名（自增），新内容写回原名。加 -y 则直接覆盖不留备份。</summary>
    Private Sub WriteWithBackup(filePath As String, data As Byte())
        If Not Opt.NoBackup Then
            Dim bak As String = NextBackupName(filePath)
            File.Move(filePath, bak)
            If Not Opt.Quiet Then Emit("  备份 → " & Path.GetFileName(bak))
        End If
        File.WriteAllBytes(filePath, data)
    End Sub

    Private Function NextBackupName(filePath As String) As String
        Dim dir As String = Path.GetDirectoryName(filePath)
        Dim name As String = Regex.Replace(Path.GetFileName(filePath), "^\d{3}_", "")
        For i As Integer = 1 To 999
            Dim cand As String = Path.Combine(dir, i.ToString("000") & "_" & name)
            If Not File.Exists(cand) AndAlso Not Directory.Exists(cand) Then Return cand
        Next
        Throw New IOException("备份序号已用尽（001-999）")
    End Function

    ''' <summary>boardinfo 的伴随哈希文件：HMACSHA256(HW_D2_Key) 作用于文件内容。</summary>
    Private Sub WriteHashFile(filePath As String, data As Byte())
        Using h As New Security.Cryptography.HMACSHA256(HwKeys.HW_D2_Key)
            File.WriteAllBytes(filePath & ".hash", h.ComputeHash(data))
        End Using
        If Not Opt.Quiet Then Emit("  哈希 → " & Path.GetFileName(filePath) & ".hash")
    End Sub

    ' ---------- 目标格式映射 ----------

    Private Function ParseTarget(spec As String, ByRef inner As HwFileFormat) As HwFileFormat
        Select Case If(spec, "").ToLowerInvariant()
            Case "", "s2", "02", "d2" : Return HwFileFormat.CtreeDynamic
            Case "s1", "01", "d1" : Return HwFileFormat.CtreeStatic
            Case "s4", "03", "d4" : Return HwFileFormat.CtreeD4
            Case "pkg", "pkg2" : inner = HwFileFormat.CtreeDynamic : Return HwFileFormat.DownloadPackage
            Case "pkg1" : inner = HwFileFormat.CtreeStatic : Return HwFileFormat.DownloadPackage
            Case "pkg4" : inner = HwFileFormat.CtreeD4 : Return HwFileFormat.DownloadPackage
            Case "v5" : Return HwFileFormat.BoardInfoV5
            Case "v6" : Return HwFileFormat.BoardInfoV6
            Case "bin4" : Return HwFileFormat.D4Binary
            Case "text", "raw" : Return HwFileFormat.PlainText
            ' ---- 其余目标：01 不压缩、明文导出 ----
            Case "raw1", "o1" : Return HwFileFormat.CtreeStaticRaw
            Case "btext", "otext", "p1" : Return HwFileFormat.PlainText
            Case Else : Throw New ArgumentException("不认识的 --to 取值：" & spec)
        End Select
    End Function

    ' ---------- 其它子命令 ----------

    Private Function CmdInfo(files As List(Of String)) As Integer
        For Each f As String In files
            If Not File.Exists(f) Then
                EmitError("文件不存在：" & f)
                Continue For
            End If
            Dim buf As Byte() = File.ReadAllBytes(f)
            Emit(String.Format("{0}{1}{2}{3}{4} 字节", Path.GetFileName(f), vbTab, HwContainer.KindName(buf, f), vbTab, buf.Length))
        Next
        Return 0
    End Function

    Private Function CmdKmc(items As List(Of String)) As Integer
        Dim ksf As Byte()
        If items.Count > 0 Then
            If Not File.Exists(items(0)) Then
                EmitError("文件不存在：" & items(0))
                Return 1
            End If
            ksf = File.ReadAllBytes(items(0))
        Else
            ksf = HwCtx.D4KeyStoreFile
        End If
        If ksf Is Nothing OrElse ksf.Length = 0 Then
            EmitError("没有可用密钥库：用 --kmc 指定，或把 kmc_store_A/B 放在程序目录")
            Return 1
        End If
        If Not HwKmc.IsKeyStore(ksf) Then
            EmitError("不是 KSFv2 密钥库")
            Return 1
        End If
        Dim list As List(Of KmcEntry) = HwKmc.GetAllMasterKeys(ksf)
        Emit("Domain" & vbTab & "Key" & vbTab & "MasterKey")
        For Each e As KmcEntry In list
            Emit(e.DomainId & vbTab & e.KeyId & vbTab & e.MasterKeyHex)
        Next
        Emit("共 " & list.Count & " 条")
        Return 0
    End Function

    Private Function CmdField(items As List(Of String)) As Integer
        If items.Count = 0 Then
            ShowHelp()
            Return 2
        End If
        Dim s As String = String.Join(" ", items).Trim()
        If s.Length > 3 AndAlso s.StartsWith("$") AndAlso s.EndsWith("$") Then
            Dim plain As String
            Select Case s.Substring(1, 1)
                Case "1" : plain = HwField.HW_D1Decrypt(s)
                Case "2" : plain = HwField.HW_D2Decrypt(s)
                Case "3" : plain = HwField.HW_D3Decrypt(s)
                Case "4" : plain = HwField.HW_D4DecryptString(s)
                Case Else : Throw New ArgumentException("不认识的字段前缀：$" & s.Substring(1, 1))
            End Select
            If plain Is Nothing OrElse plain.Length = 0 Then
                EmitError("解密失败：密钥不对或字段串损坏")
                Return 1
            End If
            Emit(plain.TrimEnd(ChrW(0)))
        Else
            Dim target As String = If(Opt.Target = "", "s2", Opt.Target)
            Select Case target
                Case "s1", "01" : Emit(HwField.HW_D1Encrypt(s, True))
                Case "s2", "02" : Emit(HwField.HW_D2Encrypt(s, True))
                Case "s4", "04" : Emit(HwField.HW_D4EncryptString(s))
                Case Else : Throw New ArgumentException("字段加密只支持 --to s1 / s2 / s4")
            End Select
        End If
        Return 0
    End Function

    Private Sub ShowHelp()
        Emit("HW Dollar4 mini")
        Emit("")
        Emit("用法：")
        Emit("  HwD4mini.exe <文件...>                自动：密文解密、明文加密")
        Emit("  HwD4mini.exe dec <文件...>            强制解密")
        Emit("  HwD4mini.exe enc <文件...> [--to X]   强制加密")
        Emit("  HwD4mini.exe info <文件...>           只识别格式")
        Emit("  HwD4mini.exe field <字符串>           字段串解密；非 $ 开头则加密")
        Emit("  HwD4mini.exe kmc [密钥库]             列出 Master Key")
        Emit("")
        Emit("选项：")
        Emit("  --to s1|s2|s4                     01 / 02 / 03，默认 s2")
        Emit("       pkg|pkg1|pkg4|v5|v6|bin4        下载包（内层 02 / 01 / 03）/ boardinfo V5 / V6 / 裸块")
        Emit("       text|btext|otext|raw1            导出明文 / 01 容器但不压缩")
        Emit("  --key <HEX>                          指定 $3/$4 主密钥（32 字节 HEX）")
        Emit("  --pass <口令>                        人工口令：解密时用于内层 $4；")
        Emit("                                       加密 pkg4 时用它派生密钥并写进包头")
        Emit("  --pmode 1|2                          加密 pkg4 的口令模式，默认 2（先派生）")
        Emit("  --psalt <盐>                         加密 pkg4 的策略盐，默认随机 24 字节")
        Emit("  --kmc <路径>                         指定密钥库文件或目录")
        Emit("  --d2key os|boardinfo|omci            $2 容器密钥盐，默认 os")
        Emit("  -y                                   不备份，直接覆盖")
        Emit("  -f                                   允许对密文二次加密（默认拒绝）")
        Emit("  -q                                   安静模式")
        Emit("")
        Emit("落盘：原文件改名为 001_原名（重名自增），结果写回原名。")
    End Sub

End Module
