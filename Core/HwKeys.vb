' 华为 ONT 固定密钥常量。全部是逆向得到的静态值，与机型 / 固件版本无关。
' 同一密钥在不同代产品里复用，故集中于此，Lite 与 Mini 共用。
Imports System.Text

Public Module HwKeys

    ''' <summary>
    ''' $1 与默认静态容器（01 头）使用的 aescrypt2 用户密钥。Keystore D4K1。
    ''' 带 "hex:" 前缀，原样交给 HwAescrypt2 —— 该前缀是密钥字面量的一部分，绝不能 hex 解码。
    ''' </summary>
    Public ReadOnly HW_CTREE_Key As String = "hex:13395537D2730554A176799F6D56A239"

    ''' <summary>$1 字段串的 ECB 轮密钥基底（Keystore D1K1，32 字节）。每块用末 4 字符改写 4 个字节。</summary>
    Public ReadOnly HW_D1_Key As Byte() = HwBytes.HexToBytes("B8363C9B77DAED4B9ABB9F2F6DF5F1D5CB64975D5D3BCEE8827F2F42235F9229")

    ''' <summary>
    ''' $2 动态码主密钥（Keystore D1K2，32 字节 ASCII）。
    ''' 同时兼作 aescrypt2 的 userKey、HMACSHA256 的 key、以及 $2 容器密钥串的加密密钥。
    ''' </summary>
    Public ReadOnly HW_D2_Key As Byte() = Encoding.UTF8.GetBytes("9jK0lk5kLmxn8sjojW962llHY76xAc2z")

    ''' <summary>
    ''' $3 内置主密钥（32 字节 ASCII，作为 PBKDF2 口令原样使用）。
    ''' 来源：HW FW Toolkit 的 XmlKeys.D3BuiltinMasterKey。上游 HW Dollar4 没有这一项，
    ''' 它取不到密钥库就直接弹窗手输；Mini 没有 UI，所以必须先试内置再考虑手输。
    ''' </summary>
    Public ReadOnly D3_Builtin_MasterKey As Byte() = Encoding.UTF8.GetBytes("d19bb451dd33b3b3cf4b60525a39fc1f")

    ''' <summary>$2 容器密钥串的可选盐。OS 为默认；BOARDINFO / OMCI 用于对应上下文。</summary>
    Public Function D2SaltFor(keyType As String) As String
        Dim t As String = If(keyType, "").ToUpper()
        If t = "BOARDINFO" Then Return "BOARDINFO(lmV1L8"       'SPEC_BOARDINFO_AES_CBC_APP_STR from libsmp_api.so
        If t = "OMCI" Then Return "asdfghjkl!@#0320"            'SPEC_OMCI_AES_CBC_APP_STR from omci.elf
        Return "Df7!ui%s9(lmV1L8"                               'SPEC_OS_AES_CBC_APP_STR from libhw_ssp_basic.so
    End Function

    ''' <summary>KSFv2 密钥库文件头魔数（34 字节）。</summary>
    Public ReadOnly KSFv2_Magic As Byte() = New Byte() {&H5F, &H64, &H97, &H8D, &H19, &H4F, &H89, &HCF, &HA8, &H3F, &H8E, &HE1, &HDB, &H1, &H3C, &HC, &H88, &H42, &H4A, &H1C, &HB7, &HFC, &HAD, &H70, &H4E, &H45, &H13, &HA5, &H14, &H46, &H71, &H6C, &H0, &H2}

    ''' <summary>KSFv2 根密钥掩码（32 字节）。</summary>
    Public ReadOnly KSFv2_Mask As Byte() = New Byte() {&HB2, &HA1, &HC, &H73, &H52, &H73, &H76, &HA1, &H60, &H62, &H2E, &H8, &H52, &H8, &H2E, &HA9, &H60, &HBC, &H2E, &H73, &H52, &HB, &HC, &HBC, &HEE, &HA, &H2E, &H8, &H52, &H9C, &H76, &HA9}

End Module
