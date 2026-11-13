' 配置文件容器识别 / 拆包 / 打包。
'
' 一张表看清所有形态（偏移 0 的魔数决定一切）：
'   01 00 00 00  静态 aescrypt2 容器，密钥 HW_CTREE_Key
'   02 00 00 00  动态 aescrypt2 容器，密钥串内嵌在偏移 12，本身还是 $2 字段串
'   03 00 00 00  $4 容器，后接 64 字节 GCM 头
'   20 22 07 09  $4 裸块（boardinfo 用，没有 03 头）
'   07 12 21 20  下载配置包，328 字节头 + 内层容器
'   1B 5C 9F 3A  boardinfo V5 头（小端，20 字节）
'   3A 9F 5C 1B  boardinfo V6 头（大端，20 字节）
'   67 66 63 71  CTCE8/CFG 包（"fgcq"），32 字节头 + 标准 GZip 流
Imports System
Imports System.IO
Imports System.Text

Public Enum HwFileFormat
    Unknown = 0
    PlainText
    FieldString
    CtreeStatic
    CtreeDynamic
    CtreeD4
    ''' <summary>01 头 + aescrypt2 + <b>不</b> gzip。customizepara.txt / encrypt_spec_key.tar.gz 就是这种。</summary>
    CtreeStaticRaw
    D4Binary
    DownloadPackage
    BoardInfoV5
    BoardInfoV6
    Ctce8Cfg
    KsfV2
    GzipStream
End Enum

Public Class HwOpenResult
    Public Property Success As Boolean
    Public Property Text As String
    Public Property SourceFormat As HwFileFormat = HwFileFormat.Unknown
    Public Property InnerFormat As HwFileFormat = HwFileFormat.Unknown
    Public Property ErrorMessage As String = ""
End Class

Public Module HwContainer

    Public Function FormatName(f As HwFileFormat) As String
        Select Case f
            Case HwFileFormat.PlainText : Return "明文"
            Case HwFileFormat.FieldString : Return "$字段串"
            Case HwFileFormat.CtreeStatic : Return "01 静态容器"
            Case HwFileFormat.CtreeDynamic : Return "02 动态容器"
            Case HwFileFormat.CtreeD4 : Return "03 $4 容器"
            Case HwFileFormat.CtreeStaticRaw : Return "01 静态容器（不压缩）"
            Case HwFileFormat.D4Binary : Return "20 22 07 09 $4 裸块"
            Case HwFileFormat.DownloadPackage : Return "07 12 21 20 下载包"
            Case HwFileFormat.BoardInfoV5 : Return "boardinfo V5"
            Case HwFileFormat.BoardInfoV6 : Return "boardinfo V6"
            Case HwFileFormat.Ctce8Cfg : Return "CFG 包 (fgcq)"
            Case HwFileFormat.KsfV2 : Return "KSFv2 密钥库"
            Case HwFileFormat.GzipStream : Return "GZip 流"
            Case Else : Return "未知"
        End Select
    End Function

    ''' <summary>按魔数识别格式。BOM 会被跳过，避免带 BOM 的明文 XML 被误判。</summary>
    Public Function DetectFormat(buf As Byte()) As HwFileFormat
        If buf Is Nothing OrElse buf.Length = 0 Then Return HwFileFormat.Unknown
        Dim off As Integer = BomLength(buf)
        Dim n As Integer = buf.Length

        If n - off >= 4 Then
            ' "fgcq" 与 "gfcq" 两种字节序都见过（上游代码判的是 0x67 0x66），统一接受，靠后面 CRC 兜底
            Dim b0 As Byte = buf(off)
            Dim b1 As Byte = buf(off + 1)
            If ((b0 = &H66 AndAlso b1 = &H67) OrElse (b0 = &H67 AndAlso b1 = &H66)) AndAlso buf(off + 2) = &H63 AndAlso buf(off + 3) = &H71 Then Return HwFileFormat.Ctce8Cfg
            If buf(off) = &H7 AndAlso buf(off + 1) = &H12 AndAlso buf(off + 2) = &H21 AndAlso buf(off + 3) = &H20 Then Return HwFileFormat.DownloadPackage
            If (buf(off) = &H1 OrElse buf(off) = &H2 OrElse buf(off) = &H3) AndAlso buf(off + 1) = 0 AndAlso buf(off + 2) = 0 AndAlso buf(off + 3) = 0 Then
                If buf(off) = &H1 Then Return HwFileFormat.CtreeStatic
                If buf(off) = &H2 Then Return HwFileFormat.CtreeDynamic
                Return HwFileFormat.CtreeD4
            End If
            If buf(off) = &H20 AndAlso buf(off + 1) = &H22 AndAlso buf(off + 2) = &H7 AndAlso buf(off + 3) = &H9 Then Return HwFileFormat.D4Binary
            If buf(off) = &H1F AndAlso buf(off + 1) = &H8B Then Return HwFileFormat.GzipStream
        End If

        If n - off >= 8 Then
            If buf(off) = &H1B AndAlso buf(off + 1) = &H5C AndAlso buf(off + 2) = &H9F AndAlso buf(off + 3) = &H3A AndAlso buf(off + 4) = &H12 AndAlso buf(off + 5) = &H3 AndAlso buf(off + 6) = &H20 AndAlso buf(off + 7) = &H20 Then Return HwFileFormat.BoardInfoV5
            If buf(off) = &H3A AndAlso buf(off + 1) = &H9F AndAlso buf(off + 2) = &H5C AndAlso buf(off + 3) = &H1B AndAlso buf(off + 4) = &H20 AndAlso buf(off + 5) = &H20 AndAlso buf(off + 6) = &H3 AndAlso buf(off + 7) = &H12 Then Return HwFileFormat.BoardInfoV6
        End If

        If HwKmc.IsKeyStore(buf) Then Return HwFileFormat.KsfV2

        Dim head As String = PeekText(buf, 4)
        If head.StartsWith("$1") OrElse head.StartsWith("$2") OrElse head.StartsWith("$3") OrElse head.StartsWith("$4") Then Return HwFileFormat.FieldString
        Return HwFileFormat.PlainText
    End Function

    ''' <summary>
    ''' 识别结果的完整称呼 —— 回答「这个文件是什么」。
    ''' 比 DetectFormat 多走几步：01 容器里压没压、03 块头是不是静态的 (1,2)、
    ''' 下载包内层是 02 还是 03、明文属于 ctree / boardinfo / 其它。
    ''' 不吃密钥库、不弹口令：01 容器用内置 $2 静态钥匙解一层就能看出有没有 gzip。
    ''' 判不出来就退回 FormatName，绝不让一个名字把调用方卡住。
    ''' </summary>
    Public Function KindName(buf As Byte(), Optional fileName As String = Nothing) As String
        If buf Is Nothing OrElse buf.Length = 0 Then Return "无法识别"
        Dim f As HwFileFormat = DetectFormat(buf)
        Try
            Select Case f
                Case HwFileFormat.CtreeStatic
                    Return If(Ctree01IsGzip(buf), "Ctree $2 Static", "Other $2 Encrypt")
                Case HwFileFormat.CtreeDynamic
                    Return "Ctree $2 Dynamic"
                Case HwFileFormat.CtreeD4
                    Return If(D4HeaderIsStatic(buf), "Ctree $4 Static", "Ctree $4 Dynamic")
                Case HwFileFormat.D4Binary
                    Return "Boardinfo $4"
                Case HwFileFormat.BoardInfoV5
                    Return "Boardinfo V5"
                Case HwFileFormat.BoardInfoV6
                    Return "Boardinfo V6"
                Case HwFileFormat.DownloadPackage
                    Return If(WrappedIsD4(buf), "Download $4", "Download $2")
                Case HwFileFormat.PlainText
                    Return PlainKind(buf, fileName)
                Case Else
                    Return FormatName(f)
            End Select
        Catch ex As Exception
            ' 认不出就当最简单级名字，绝不因为命名抛异常把调用方卡住
            System.Diagnostics.Debug.WriteLine("[忽略] KindName: " & ex.Message)
            Return FormatName(f)
        End Try
    End Function

    ''' <summary>01 容器（8 字节头 + aescrypt2）解一层，看明文头两字节是不是 gzip 魔数 1F 8B。</summary>
    Private Function Ctree01IsGzip(buf As Byte()) As Boolean
        Dim off As Integer = BomLength(buf)
        If buf.Length <= off + 8 Then Return True
        Dim plain As Byte() = HwAescrypt2.TryDecrypt(buf, off + 8, buf.Length - off - 8,
                                                     Encoding.UTF8.GetBytes(HwKeys.HW_CTREE_Key))
        Return plain IsNot Nothing AndAlso plain.Length >= 2 AndAlso plain(0) = &H1F AndAlso plain(1) = &H8B
    End Function

    ''' <summary>03 容器：(Domain,Key) 在块内 8..11，块从文件偏移 8 开始，所以看 16..19。</summary>
    Private Function D4HeaderIsStatic(buf As Byte()) As Boolean
        Dim off As Integer = BomLength(buf)
        If buf.Length < off + 20 Then Return True
        Return buf(off + 16) = 0 AndAlso buf(off + 17) = 1 AndAlso buf(off + 18) = 0 AndAlso buf(off + 19) = 2
    End Function

    ''' <summary>下载包内层从偏移 328 起，首字节就是版本号：2 = 02 容器，3 = 03 容器。</summary>
    Private Function WrappedIsD4(buf As Byte()) As Boolean
        Dim off As Integer = BomLength(buf)
        If buf.Length <= off + 328 Then Return False
        Return buf(off + 328) = 3
    End Function

    ''' <summary>明文再分三类：以 &lt; 开头当 ctree XML，含 obj.id 当 boardinfo，其余算其它文件。</summary>
    ''' <summary>
    ''' 明文分三类：ctree 配置 / boardinfo / 其它。
    ''' 只看内容不够 —— 万一明文 XML 是空壳、被截断、或者开头先摆一堆注释，头 512 字节里
    ''' 可能一个 '&lt;' 都没有，就掉进 Other Decrypt 了。所以扩展名也参与判断：
    ''' 设备上未加密的配置文件就叫 hw_ctree*.xml，.xml 后缀本身就是强证据。
    ''' </summary>
    Private Function PlainKind(buf As Byte(), fileName As String) As String
        Dim n As Integer = Math.Min(buf.Length, 512)
        If n <= 0 Then Return "Other Decrypt"
        Dim head As String = Encoding.UTF8.GetString(buf, 0, n).TrimStart(ChrW(&HFEFF), " "c, ChrW(13), ChrW(10), ChrW(9))
        If head.StartsWith("<") Then Return "Ctree Decrypt"
        ' boardinfo 明文的特征是那串 obj.id，跟扩展名无关（设备上叫 hw_boardinfo，备份里常是 .txt）
        If head.IndexOf("obj.id", StringComparison.Ordinal) >= 0 Then Return "Boardinfo Decrypt"
        ' 内容是明文但看不出 XML 特征，用扩展名兜底
        If If(fileName, "").ToLowerInvariant().EndsWith(".xml") Then Return "Ctree Decrypt"
        Return "Other Decrypt"
    End Function

    ''' <summary>自动拆壳 + 解密。返回明文；失败时 Success=False 并带原因。</summary>
    Public Function OpenConfig(buf As Byte()) As HwOpenResult
        Dim r As New HwOpenResult
        Try
            Dim fmt As HwFileFormat = DetectFormat(buf)
            r.SourceFormat = fmt
            Dim data As Byte() = buf

            ' 最多剥两层壳：下载包里的内层还可以再是 CTCE8
            For depth As Integer = 0 To 2
                If fmt = HwFileFormat.Ctce8Cfg Then
                    data = UnwrapCtce8(data)
                    fmt = DetectFormat(data)
                ElseIf fmt = HwFileFormat.DownloadPackage Then
                    data = UnwrapDownload(data)
                    fmt = DetectFormat(data)
                Else
                    Exit For
                End If
            Next
            r.InnerFormat = fmt

            Select Case fmt
                Case HwFileFormat.CtreeStatic, HwFileFormat.CtreeDynamic, HwFileFormat.CtreeD4
                    r.Text = DecryptCtree(data, fmt)
                Case HwFileFormat.D4Binary
                    Dim plain As Byte() = HwField.HW_D4Decrypt(data)
                    If plain.Length = 0 Then Throw New InvalidDataException("$4 解密失败：密钥不对或数据损坏")
                    r.Text = Encoding.UTF8.GetString(plain)
                Case HwFileFormat.BoardInfoV5, HwFileFormat.BoardInfoV6
                    r.Text = DecryptBoardInfo(data, fmt)
                Case HwFileFormat.GzipStream
                    r.Text = Encoding.UTF8.GetString(HwGzip.UnGZip(data))
                Case HwFileFormat.FieldString
                    r.Text = DecryptFieldText(StripBom(Encoding.UTF8.GetString(data)).Trim())
                Case Else
                    r.Text = StripBom(Encoding.UTF8.GetString(data))
            End Select

            r.Success = (r.Text IsNot Nothing AndAlso r.Text.Length > 0)
            If Not r.Success Then r.ErrorMessage = "解密结果为空，可能是密钥不匹配"
        Catch ex As Exception
            r.Success = False
            r.ErrorMessage = ex.Message
        End Try
        Return r
    End Function

    ' ---------- 拆包 ----------

    Private Function UnwrapCtce8(data As Byte()) As Byte()
        If data.Length < 36 Then Throw New InvalidDataException("CFG 包头不完整")
        Dim payload(data.Length - 33) As Byte
        Array.Copy(data, 32, payload, 0, data.Length - 32)
        Dim crcStored As String = HwBytes.BytesToHex(New Byte() {data(4), data(5), data(6), data(7)})
        Dim crcCalc As String = HwBytes.BytesToHex(BitConverter.GetBytes(HwCrc.CRC32(payload)))
        If crcStored <> crcCalc Then Throw New InvalidDataException("CFG 包 CRC 校验失败")
        ' payload 是标准 GZip 流（尾部自带 ISIZE），不是前置长度格式
        Return HwGzip.UnGZip(payload)
    End Function

    Private Function UnwrapDownload(data As Byte()) As Byte()
        If data.Length < 328 Then Throw New InvalidDataException("下载包不足 328 字节")
        If HwBytes.GetLittleEndian32(data, 52) <> 328UI Then Throw New InvalidDataException("下载包头长不是 328")
        Dim innerLen As Integer = CInt(HwBytes.GetLittleEndian32(data, 56))
        If innerLen <= 0 OrElse 328 + innerLen > data.Length Then Throw New InvalidDataException("下载包内层长度越界")
        Dim inner(innerLen - 1) As Byte
        Array.Copy(data, 328, inner, 0, innerLen)

        Dim head(39) As Byte
        Array.Copy(data, 64, head, 0, 40)
        ' CRC 是无符号值，绝不能 CInt 比较 —— 超过 2^31 会抛算术溢出
        If HwBytes.GetLittleEndian32(data, 320) <> HwCrc.CRC32(inner) Then Throw New InvalidDataException("下载包内层 CRC 校验失败")
        If HwBytes.GetLittleEndian32(data, 324) <> HwCrc.CRC32(head) Then Throw New InvalidDataException("下载包头部 CRC 校验失败")

        ' 包头 96/100 是 DomainID / KeyID（小端 32 位）。内层 03 块里可能写 0/0，
        ' 这里预置给 HW_D4Decrypt，它靠这个去密钥库取主密钥。
        Dim dom As UInteger = HwBytes.GetLittleEndian32(data, 96)
        Dim kid As UInteger = HwBytes.GetLittleEndian32(data, 100)
        HwCtx.PolicyDomainId = CInt(dom)
        HwCtx.PolicyKeyId = CInt(kid)
        If dom <> 0UI OrElse kid <> 0UI Then
            HwCtx.D4KeyStore = New Byte() {CByte((dom >> 8) And &HFFUI), CByte(dom And &HFFUI), CByte((kid >> 8) And &HFFUI), CByte(kid And &HFFUI)}
        End If

        ' 人工口令相关的三个字段。出厂人工设过密码的包，内层 03 就是靠这套解的，
        ' 跟设备 kmc_store 里的主密钥毫无关系 —— 这也是它用「密码」而不是「主密钥」的原因。
        HwCtx.PolicyMode = CInt(HwBytes.GetLittleEndian32(data, 4))
        If HwCtx.PolicyMode = 2 Then
            ' 偏移 12 起、遇 0x00 结束。上游直接用 Array.IndexOf，这里加长度护栏，
            ' 避免 12 处就是 0 时算出负长度。
            Dim z As Integer = -1
            Dim limit As Integer = Math.Min(data.Length, 12 + 64)
            For i As Integer = 12 To limit - 1
                If data(i) = 0 Then
                    z = i
                    Exit For
                End If
            Next
            If z > 12 Then
                Dim salt(z - 12 - 1) As Byte
                Array.Copy(data, 12, salt, 0, salt.Length)
                HwCtx.PolicySalt = salt
            Else
                HwCtx.PolicySalt = New Byte() {}
            End If
        Else
            HwCtx.PolicySalt = New Byte() {}
        End If

        Return inner
    End Function

    Private Function DecryptCtree(data As Byte(), fmt As HwFileFormat) As String
        Dim payloadStart As Integer = 8
        Dim key As Byte()

        Select Case fmt
            Case HwFileFormat.CtreeStatic
                key = Encoding.UTF8.GetBytes(HwKeys.HW_CTREE_Key)
            Case HwFileFormat.CtreeDynamic
                Dim keyLen As Integer = CInt(HwBytes.GetLittleEndian32(data, 8))
                If keyLen <= 0 OrElse 12 + keyLen > data.Length Then Throw New InvalidDataException("02 容器密钥长度越界")
                Dim keyStr As String = Encoding.UTF8.GetString(data, 12, keyLen)
                key = Encoding.UTF8.GetBytes(HwField.HW_D2Decrypt(keyStr))
                payloadStart = 12 + keyLen
            Case HwFileFormat.CtreeD4
                Dim d4(data.Length - 9) As Byte
                Array.Copy(data, 8, d4, 0, d4.Length)
                Dim plain As Byte() = HwField.HW_D4Decrypt(d4)
                If plain.Length = 0 Then Throw New InvalidDataException("$4 解密失败：密钥不对或数据损坏")
                Return Encoding.UTF8.GetString(HwGzip.UnGZip(plain))
            Case Else
                Throw New InvalidDataException("非 ctree 容器")
        End Select

        Dim payload(data.Length - payloadStart - 1) As Byte
        Array.Copy(data, payloadStart, payload, 0, payload.Length)
        Dim crcStored As String = HwBytes.BytesToHex(New Byte() {data(4), data(5), data(6), data(7)})
        Dim crcCalc As String = HwBytes.BytesToHex(HwCrc.HW_CTREE_CRC32(payload))
        If crcStored <> crcCalc Then Throw New InvalidDataException("容器 CRC 校验失败")

        Dim dec As Byte() = HwAescrypt2.TryDecrypt(payload, key)
        If dec Is Nothing OrElse dec.Length = 0 Then Throw New InvalidDataException("aescrypt2 解密失败：密钥不对或数据损坏")
        If dec.Length > 1 AndAlso dec(0) = &H1F AndAlso dec(1) = &H8B Then dec = HwGzip.UnGZip(dec)
        Return Encoding.UTF8.GetString(dec)
    End Function

    Private Function DecryptBoardInfo(data As Byte(), fmt As HwFileFormat) As String
        If data.Length <= 20 Then Throw New InvalidDataException("boardinfo 内容过短")
        Dim payload(data.Length - 21) As Byte
        Array.Copy(data, 20, payload, 0, payload.Length)
        Dim txt As String = StripBom(Encoding.UTF8.GetString(payload)).TrimEnd(ChrW(0))
        ' boardinfo 的载荷本身往往又是一条 $2/$4 字段串
        If txt.StartsWith("$") Then Return DecryptFieldText(txt.Trim())
        Return txt
    End Function

    ''' <summary>解 $1/$2/$3/$4 字段串。</summary>
    Public Function DecryptFieldText(s As String) As String
        Dim t As String = s.Trim()
        If t.Length < 4 OrElse Not t.StartsWith("$") Then Return s
        Select Case t.Substring(1, 1)
            Case "1" : Return HwField.HW_D1Decrypt(t)
            Case "2" : Return HwField.HW_D2Decrypt(t)
            Case "3" : Return HwField.HW_D3Decrypt(t)
            Case "4" : Return HwField.HW_D4DecryptString(t)
            Case Else : Return s
        End Select
    End Function

    ' ---------- 打包 ----------

    ''' <summary>按目标格式打包。CtreeDynamic / CtreeD4 / D4Binary 需要先设好 HwCtx 里的密钥。</summary>
    Public Function BuildConfig(text As String, target As HwFileFormat) As Byte()
        Dim raw As Byte() = Encoding.UTF8.GetBytes(text)
        Select Case target
            Case HwFileFormat.CtreeStatic
                Return WrapCtree(HwAescrypt2.Encrypt(HwGzip.GZip(raw), Encoding.UTF8.GetBytes(HwKeys.HW_CTREE_Key)), 1, Nothing)
            Case HwFileFormat.CtreeStaticRaw
                ' 同 01 容器、同一把 $2 静态钥匙，只是 payload 不压 —— customizepara.txt /
                ' encrypt_spec_key.tar.gz 这类原样加密的文件就是它。解密侧靠 1F 8B 魔数
                ' 自己判断要不要解压，所以两种 01 容器用的是同一条读路径。
                Return WrapCtree(HwAescrypt2.Encrypt(raw, Encoding.UTF8.GetBytes(HwKeys.HW_CTREE_Key)), 1, Nothing)
            Case HwFileFormat.CtreeDynamic
                Return WrapCtree(HwAescrypt2.Encrypt(HwGzip.GZip(raw), HwKeys.HW_D2_Key), 2, HwKeys.HW_D2_Key)
            Case HwFileFormat.CtreeD4
                Dim body As Byte() = HwField.HW_D4Encrypt(HwGzip.GZip(raw))
                Return WrapCtree(body, 3, Nothing)
            Case HwFileFormat.D4Binary
                Return HwField.HW_D4Encrypt(raw)
            Case HwFileFormat.BoardInfoV5
                Return WrapBoardInfo(raw, 5)
            Case HwFileFormat.BoardInfoV6
                Return WrapBoardInfo(raw, 6)
            Case HwFileFormat.PlainText
                Return raw
            Case Else
                Throw New NotSupportedException("不支持写出为：" & FormatName(target))
        End Select
    End Function

    ''' <summary>
    ''' 解包成明文，给「Ctree Decrypt / Boardinfo Decrypt / Other Decrypt」那几项目标用。
    ''' 走的是 OpenConfig 的完整拆壳 + 解密链路，所以 01/02/03 容器、下载包、boardinfo、
    ''' CTCE8 都能一把过；失败直接抛，让上层统一报错。
    ''' </summary>
    Public Function BuildPlain(buf As Byte()) As Byte()
        Dim r As HwOpenResult = OpenConfig(buf)
        If Not r.Success Then Throw New InvalidDataException(r.ErrorMessage)
        Return Encoding.UTF8.GetBytes(r.Text)
    End Function

    Private Function WrapCtree(body As Byte(), version As Byte, d2Key As Byte()) As Byte()
        Dim keyStr As Byte() = If(version = 2, Encoding.UTF8.GetBytes(HwField.HW_D2Encrypt(Encoding.UTF8.GetString(d2Key))), New Byte() {})
        Dim headLen As Integer = If(version = 2, 12 + keyStr.Length, 8)
        Dim r(headLen + body.Length - 1) As Byte
        r(0) = version
        Dim crc As Byte() = HwCrc.HW_CTREE_CRC32(body)
        Array.Copy(crc, 0, r, 4, 4)
        If version = 2 Then
            Array.Copy(BitConverter.GetBytes(Convert.ToInt64(keyStr.Length)), 0, r, 8, 4)
            Array.Copy(keyStr, 0, r, 12, keyStr.Length)
        End If
        Array.Copy(body, 0, r, headLen, body.Length)
        Return r
    End Function

    Private Function WrapBoardInfo(payload As Byte(), version As Integer) As Byte()
        Dim head(15) As Byte
        If version = 5 Then
            head(0) = &H1B : head(1) = &H5C : head(2) = &H9F : head(3) = &H3A
            head(4) = &H12 : head(5) = &H3 : head(6) = &H20 : head(7) = &H20
        Else
            head(0) = &H3A : head(1) = &H9F : head(2) = &H5C : head(3) = &H1B
            head(4) = &H20 : head(5) = &H20 : head(6) = &H3 : head(7) = &H12
        End If
        Dim lenBytes As Byte() = BitConverter.GetBytes(Convert.ToInt64(payload.Length))
        Dim crcBytes As Byte() = BitConverter.GetBytes(HwCrc.CRC32(payload))
        For i As Integer = 0 To 3
            If version = 5 Then
                head(8 + i) = lenBytes(i)
                head(12 + i) = crcBytes(i)
            Else
                head(8 + i) = lenBytes(3 - i)
                head(12 + i) = crcBytes(3 - i)
            End If
        Next
        Dim r(payload.Length + 19) As Byte
        Array.Copy(head, 0, r, 0, 16)
        Dim headCrc As Byte() = BitConverter.GetBytes(HwCrc.CRC32(head))
        For i As Integer = 0 To 3
            r(16 + i) = If(version = 5, headCrc(i), headCrc(3 - i))
        Next
        Array.Copy(payload, 0, r, 20, payload.Length)
        Return r
    End Function

    ''' <summary>
    ''' 把内层容器包成 07 12 21 20 下载包。
    ''' 偏移 96/100 的 Domain/Key 只在内层是 03 容器时才有意义，其余情况写 1/2 占位
    ''' （上游是无条件写 D4KeyStore，导致 $2 包里混入残留值）。
    '''
    ''' 偏移 4（口令模式）与偏移 12（策略盐）由 HwCtx.PolicyMode / PolicySalt 决定。
    ''' 注意二者位于 64..103 的 head 之外，不参与 headCRC —— 只有 96/100 在里面，
    ''' 所以那两项必须在算 headCRC 之前写完，而口令策略写在哪一步都不影响校验。
    ''' </summary>
    Public Function BuildDownloadPackage(inner As Byte(), innerFormat As HwFileFormat) As Byte()
        Dim r(inner.Length + 327) As Byte
        r(0) = &H7 : r(1) = &H12 : r(2) = &H21 : r(3) = &H20
        r(52) = &H48
        r(53) = &H1
        Dim lenBytes As Byte() = BitConverter.GetBytes(Convert.ToInt64(inner.Length))
        r(56) = lenBytes(0) : r(57) = lenBytes(1) : r(58) = lenBytes(2) : r(59) = lenBytes(3)
        r(60) = &H1

        ' 人工口令策略。模式 0 = 不声明，解包方会退回「密钥库主密钥」那条路。
        If HwCtx.PolicyMode > 0 Then
            Dim modeBytes As Byte() = BitConverter.GetBytes(Convert.ToInt64(HwCtx.PolicyMode))
            Array.Copy(modeBytes, 0, r, 4, 4)
            If HwCtx.PolicyMode = 2 Then
                If HwCtx.PolicySalt.Length = 0 Then
                    Throw New InvalidDataException("模式 2 必须带策略盐，否则解包方无从派生")
                End If
                If HwCtx.PolicySalt.Length > HwCtx.PolicySaltMax Then
                    Throw New InvalidDataException("策略盐最长 " & HwCtx.PolicySaltMax & " 字节")
                End If
                Array.Copy(HwCtx.PolicySalt, 0, r, 12, HwCtx.PolicySalt.Length)
                ' r(12 + 长度) 本来就还是 0，终止符天然成立
            End If
        End If

        If innerFormat = HwFileFormat.CtreeD4 AndAlso HwCtx.D4KeyStore.Length >= 4 Then
            r(96) = HwCtx.D4KeyStore(1)
            r(97) = HwCtx.D4KeyStore(0)
            r(100) = HwCtx.D4KeyStore(3)
            r(101) = HwCtx.D4KeyStore(2)
        Else
            ' 低字节在前：96/97 组成 DomainID=1，100/101 组成 KeyID=2
            r(96) = &H1
            r(100) = &H2
        End If

        Dim head(39) As Byte
        Array.Copy(r, 64, head, 0, 40)
        Dim innerCrc As Byte() = BitConverter.GetBytes(HwCrc.CRC32(inner))
        Dim headCrc As Byte() = BitConverter.GetBytes(HwCrc.CRC32(head))
        Array.Copy(innerCrc, 0, r, 320, 4)
        Array.Copy(headCrc, 0, r, 324, 4)
        Array.Copy(inner, 0, r, 328, inner.Length)
        Return r
    End Function

    ' ---------- 小工具 ----------

    Private Function BomLength(buf As Byte()) As Integer
        If buf.Length >= 3 AndAlso buf(0) = &HEF AndAlso buf(1) = &HBB AndAlso buf(2) = &HBF Then Return 3
        Return 0
    End Function

    Public Function StripBom(s As String) As String
        If s Is Nothing Then Return ""
        If s.Length > 0 AndAlso AscW(s(0)) = &HFEFF Then Return s.Substring(1)
        Return s
    End Function

    Private Function PeekText(buf As Byte(), count As Integer) As String
        Dim off As Integer = BomLength(buf)
        Dim len As Integer = Math.Min(count, buf.Length - off)
        If len <= 0 Then Return ""
        Return Encoding.UTF8.GetString(buf, off, len)
    End Function

End Module
