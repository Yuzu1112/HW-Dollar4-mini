' mini 的 $4 加密独立模块。
'
' 为什么不直接用共享内核的 HwContainer.BuildConfig：
'   那条路依赖 HwCtx 的全局口令槽（D4Password）。槽被清空时（比如解一个查不到库的
'   $4 文件、用户又在口令框点了取消），后面再想加密成 $4 就会直接失败，报
'   「缺少 $4 主密钥」——内置的 (Domain,Key)=(1,2) 本来就能加，却因为状态被清而做不了。
'
' 这个模块只做一件事：给定明文，产出 $4 结构，钥匙按显式优先级解析，
' 最差也回落到出厂的 $2 密钥，所以 $4 加密永远可用。
'
' 钥匙优先级：
'   1. 调用方显式传入的 key
'   2. HwCtx.D4Password（已加载密钥库 / 刚解过带主密钥的文件）
'   3. HwKeys.HW_D2_Key —— 出厂 (1,2) 那把，即 $2 静态密钥
Imports System
Imports System.Text

Public Module HwD4Enc

    ''' <summary>解析本次加密要用的主密钥，永不返回空数组。</summary>
    Public Function ResolveKey(Optional explicitKey As Byte() = Nothing) As Byte()
        If explicitKey IsNot Nothing AndAlso explicitKey.Length > 0 Then Return explicitKey
        If HwCtx.D4Password IsNot Nothing AndAlso HwCtx.D4Password.Length > 0 Then Return HwCtx.D4Password
        Return HwKeys.HW_D2_Key
    End Function

    ''' <summary>解析要写进 03 块头的 (Domain,Key)；密钥槽没初始化时用出厂的 (0,1,0,2)。</summary>
    Public Function ResolveDomainKey() As Byte()
        If HwCtx.D4KeyStore IsNot Nothing AndAlso HwCtx.D4KeyStore.Length >= 4 Then Return HwCtx.D4KeyStore
        Return New Byte(3) {&H0, &H1, &H0, &H2}
    End Function

    ''' <summary>这次到底用了哪把钥匙，给状态行显示用。</summary>
    Public Function KeySource() As String
        If HwCtx.D4Password IsNot Nothing AndAlso HwCtx.D4Password.Length > 0 Then Return "当前主密钥"
        Return "默认 $2 密钥"
    End Function

    ''' <summary>
    ''' 03 块布局：20 22 07 09 | 4 字节保留 | Domain/Key(4) | IV(16) | Tag(16) | Salt(16) |
    ''' 密文长度(大端 4) | 密文。与设备、与 Lite 产出的块逐字节同构。
    ''' </summary>
    Public Function BuildBlock(data As Byte(), key As Byte(), domainKey As Byte()) As Byte()
        If data Is Nothing Then Return New Byte() {}
        If key Is Nothing OrElse key.Length = 0 Then Return New Byte() {}
        If domainKey Is Nothing OrElse domainKey.Length < 4 Then domainKey = New Byte(3) {&H0, &H1, &H0, &H2}

        Dim iv(15) As Byte
        Dim salt(15) As Byte
        Dim rnd As New Random
        rnd.NextBytes(iv)
        rnd.NextBytes(salt)

        Dim tag As Byte() = Nothing
        Dim cipher As Byte() = HwGcm.EncryptPbkdf2(key, salt, iv, data, tag)
        If cipher Is Nothing OrElse cipher.Length = 0 Then Return New Byte() {}

        Dim r(data.Length + 64 - 1) As Byte
        r(0) = &H20
        r(1) = &H22
        r(2) = &H7
        r(3) = &H9
        Array.Copy(domainKey, 0, r, 8, 4)
        Array.Copy(iv, 0, r, 12, 16)
        Array.Copy(tag, 0, r, 28, 16)
        Array.Copy(salt, 0, r, 44, 16)
        Dim size As Byte() = BitConverter.GetBytes(cipher.Length)
        r(60) = size(3)
        r(61) = size(2)
        r(62) = size(1)
        r(63) = size(0)
        Array.Copy(cipher, 0, r, 64, cipher.Length)
        Return r
    End Function

    ''' <summary>文本 → 03 容器（gzip 正文 + 03 块 + 8 字节头 + HW_CTREE_CRC32）。</summary>
    Public Function BuildCtree(text As String, key As Byte(), domainKey As Byte()) As Byte()
        Dim block As Byte() = BuildBlock(HwGzip.GZip(Encoding.UTF8.GetBytes(text)), key, domainKey)
        If block.Length = 0 Then Return New Byte() {}
        Return WrapCtree(block)
    End Function

    ''' <summary>文本 → 裸 03 块（不压、不加容器头），对应「Boardinfo $4」那类目标。</summary>
    Public Function BuildBinaryText(text As String, key As Byte(), domainKey As Byte()) As Byte()
        Return BuildBlock(Encoding.UTF8.GetBytes(text), key, domainKey)
    End Function

    ''' <summary>8 字节容器头：版本号 3 + 正文 CRC32，与内核 WrapCtree 的 03 分支一致。</summary>
    Private Function WrapCtree(block As Byte()) As Byte()
        Dim r(8 + block.Length - 1) As Byte
        r(0) = 3
        Dim crc As Byte() = HwCrc.HW_CTREE_CRC32(block)
        Array.Copy(crc, 0, r, 4, 4)
        Array.Copy(block, 0, r, 8, block.Length)
        Return r
    End Function

End Module
