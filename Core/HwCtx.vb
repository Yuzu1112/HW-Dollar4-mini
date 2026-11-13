' 共享运行状态 + 宿主注入点。
' Core 不允许引用任何窗体 / MessageBox：GUI 版往 PasswordProvider 里塞弹窗实现，
' CLI 版塞「读 --key 参数 / 自动找 kmc_store」实现，两者共用同一套算法路径。
Imports System

Public Module HwCtx

    ''' <summary>boardinfo 容器版本：3 = 无头/$1$2 直接写，5 = 1B5C9F3A 头，6 = 3A9F5C1B 头。</summary>
    Public BoardInfoVersion As Integer = 3

    ''' <summary>当前 $4 容器的 (DomainID, KeyID)，大端存放。HW_D4Reset 后为 {0,1,0,2}。</summary>
    Public D4KeyStore As Byte() = New Byte() {}

    ''' <summary>已加载的 kmc_store 文件内容（KSFv2 原始字节）。空数组表示未加载。</summary>
    Public D4KeyStoreFile As Byte() = New Byte() {}

    ''' <summary>上次使用的密钥库目录。CLI 下默认是 exe 所在目录。</summary>
    Public D4KeyStoreLast As String = ""

    ''' <summary>当前 $4 / $3 主密钥。空数组表示没有。</summary>
    Public D4Password As Byte() = New Byte() {}

    ''' <summary>$2 容器密钥串的盐类型选择，替代 GUI 的下拉框（OS / BOARDINFO / OMCI）。</summary>
    Public D2KeyType As String = "OS"

    ' ---------- 下载包的人工口令策略 ----------
    '
    ' 下载包（07 12 21 20）头部自带三件事，决定内层 03 块怎么解：
    '   偏移 4   口令模式：1 = 口令原样当密钥；2 = 口令先 PBKDF2-10000/16 字节再转 hex
    '   偏移 12  策略盐，遇到 0x00 结束（模式 2 才用）
    '   偏移 96/100  DomainID / KeyID，(1,2) 表示走内置默认，不需要口令
    ' 机型出厂时如果人工设过密码，走的就是这条路，跟设备 kmc_store 里的主密钥无关。

    ''' <summary>下载包偏移 4 的口令模式：0 = 未知/未设置，1 = 直用，2 = 先派生。</summary>
    Public PolicyMode As Integer = 0

    ''' <summary>下载包偏移 12 起的策略盐。模式 2 用它做 PBKDF2 的 salt。</summary>
    Public PolicySalt As Byte() = New Byte() {}

    ''' <summary>
    ''' 策略盐上限 51 字节。偏移 12 起可占到 63，但 64 往后是 40 字节 head（含 96/100 的
    ''' Domain/Key，参与 headCRC），绝不能踩；再留 1 字节 0x00 作终止符，故 63-12=51。
    ''' </summary>
    Public Const PolicySaltMax As Integer = 51

    ''' <summary>生成 24 字节随机可打印策略盐，长度对齐真实样本（2CjBrPFBIVPJHqannXpJHP5H）。</summary>
    Public Function MakePolicySalt() As Byte()
        Const alphabet As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
        Dim rng As New Security.Cryptography.RNGCryptoServiceProvider()
        Dim pick(23) As Byte
        rng.GetBytes(pick)
        Dim sb As New Text.StringBuilder(24)
        For Each b As Byte In pick
            sb.Append(alphabet(CInt(b) Mod alphabet.Length))
        Next
        Return Text.Encoding.UTF8.GetBytes(sb.ToString())
    End Function

    ''' <summary>用户显式提供的人工口令（原始文本）。空串表示没提供。</summary>
    Public PolicyPassphrase As String = ""

    ''' <summary>下载包头部声明的 DomainID / KeyID，-1 表示没读到。</summary>
    Public PolicyDomainId As Integer = -1
    Public PolicyKeyId As Integer = -1

    ''' <summary>
    ''' 按下载包声明的模式，把人工口令派生成 $4 主密钥。
    ''' 模式 1 直接返回口令的 UTF8 字节（长度可以不是 32，PBKDF2 接受任意长度）；
    ''' 模式 2 返回 hex(PBKDF2(pw, 策略盐, 10000, 16)).lower() 的 32 字节 ASCII。
    ''' </summary>
    Public Function DerivePolicyPassword() As Byte()
        If String.IsNullOrEmpty(PolicyPassphrase) Then Return New Byte() {}
        Dim pw As Byte() = Text.Encoding.UTF8.GetBytes(PolicyPassphrase)
        If PolicyMode = 2 AndAlso PolicySalt.Length > 0 Then
            Dim k16 As Byte() = HwGcm.Pbkdf2(pw, PolicySalt, 10000, 16)
            Return Text.Encoding.UTF8.GetBytes(HwBytes.BytesToHex(k16).ToLowerInvariant())
        End If
        Return pw
    End Function


    ''' <summary>
    ''' 取主密钥的宿主钩子。入参为期望的密钥库目录（可为空串）。
    ''' 返回 True 表示 D4Password 已就绪，False 表示放弃。
    ''' CLI 版不设置此钩子，改由显式 --key / --kmc 参数提供。
    ''' </summary>
    Public PasswordProvider As Func(Of String, Boolean) = Nothing

    ''' <summary>调用宿主钩子；没有钩子时直接返回 False（不弹窗、不阻塞）。</summary>
    Public Function PromptPassword(Optional dir As String = "") As Boolean
        If PasswordProvider Is Nothing Then Return False
        Return PasswordProvider(dir)
    End Function

    ''' <summary>重置 $4 状态到「无密钥库、内置默认 Domain/Key」的初始态。</summary>
    Public Sub HwD4Reset()
        D4Password = HwKeys.HW_D2_Key
        D4KeyStore = New Byte(3) {&H0, &H1, &H0, &H2}
        D4KeyStoreFile = New Byte() {}
    End Sub

End Module
