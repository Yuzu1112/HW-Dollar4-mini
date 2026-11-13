' HW Dollar4 mini 主窗体 —— 控件构建部分。
'
' 这个文件必须保持「设计器可解析」的标准格式：
'   只允许 Me.X = New T() 和 Me.X.属性 = 常量表达式，
'   不能写局部变量、辅助方法调用、循环、If —— 否则 VS 打开设计器会报
'   「未能加载基类 System.Windows.Forms.Form」。布局数据由 gen_designer.py 生成。
'
' 两张功能卡片：洋红铺满窗体背景，卡片用浅粉白（RoundBox 4px 圆角）；
' 每行输入是一个 RoundBox 外壳（高 32），里面装无边框 TextBox 和右侧按钮，
' 于是按钮视觉上在输入框内部，且所有行高严格一致。
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MiniForm
    Inherits System.Windows.Forms.Form

    ' 窗体重写 Dispose，清理组件列表
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    Public Sub New()
        MyBase.New()
        ' 设计器要求：构造里必须调用 InitializeComponent，
        ' 少了它窗体是空的（这条在 VB 模板里由设计器自动写）
        InitializeComponent()
    End Sub

    ' 注意：本方法由设计器与 gen_designer.py 共同维护，请勿手工插入逻辑
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.LblTitle = New System.Windows.Forms.Label()
        Me.PnlCfg = New RoundBox()
        Me.LblCfgTitle = New System.Windows.Forms.Label()
        Me.PnlTypeTag = New RoundBox()
        Me.LblTypeTag = New System.Windows.Forms.Label()
        Me.PnlFileShell = New RoundBox()
        Me.PnlKsfShell = New RoundBox()
        Me.PnlPassShell = New RoundBox()
        Me.TxtCfgFile = New System.Windows.Forms.TextBox()
        Me.BtnCfgBrowse = New RoundButton()
        Me.TxtKmc = New System.Windows.Forms.TextBox()
        Me.BtnKmcBrowse = New RoundButton()
        Me.CmbTarget = New RoundCombo()
        Me.BtnDecrypt = New RoundButton()
        Me.BtnEncrypt = New RoundButton()
        Me.TxtPass = New System.Windows.Forms.TextBox()
        Me.PnlField = New RoundBox()
        Me.LblFieldTitle = New System.Windows.Forms.Label()
        Me.PnlFieldInShell = New RoundBox()
        Me.PnlFieldOutShell = New RoundBox()
        Me.TxtFieldIn = New System.Windows.Forms.TextBox()
        Me.TxtFieldOut = New System.Windows.Forms.TextBox()
        Me.BtnCopyField = New RoundButton()
        Me.BtnFieldAuto = New RoundButton()
        Me.BtnFieldD2B = New RoundButton()
        Me.BtnFieldD2O = New RoundButton()
        Me.BtnFieldE1 = New RoundButton()
        Me.BtnFieldE2 = New RoundButton()
        Me.BtnFieldE4 = New RoundButton()
        Me.PnlMsgShell = New RoundBox()
        Me.TxtMsg = New System.Windows.Forms.TextBox()
        Me.PnlCfg.SuspendLayout()
        Me.PnlField.SuspendLayout()
        Me.PnlMsgShell.SuspendLayout()
        Me.PnlTypeTag.SuspendLayout()
        Me.PnlFileShell.SuspendLayout()
        Me.PnlKsfShell.SuspendLayout()
        Me.PnlPassShell.SuspendLayout()
        Me.PnlFieldInShell.SuspendLayout()
        Me.PnlFieldOutShell.SuspendLayout()
        Me.SuspendLayout()

        ' ---- 标题：直接压在洋红底上，居中 ----
        Me.LblTitle.AutoSize = False
        Me.LblTitle.BackColor = System.Drawing.Color.Transparent
        Me.LblTitle.Font = New System.Drawing.Font("微软雅黑", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.LblTitle.ForeColor = System.Drawing.Color.White
        Me.LblTitle.Location = New System.Drawing.Point(0, 14)
        Me.LblTitle.Size = New System.Drawing.Size(760, 34)
        Me.LblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.LblTitle.Text = "HW Dollar4 mini 版本: 4.44.0 (2026.11)"

        ' ================= 卡片一：配置文件 =================
        Me.PnlCfg.BackColor = System.Drawing.Color.MediumVioletRed
        Me.PnlCfg.FillColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlCfg.BorderColor = System.Drawing.Color.Empty
        Me.PnlCfg.Radius = 6
        Me.PnlCfg.Location = New System.Drawing.Point(12, 62)
        Me.PnlCfg.Size = New System.Drawing.Size(736, 204)

        Me.LblCfgTitle.AutoSize = False
        Me.LblCfgTitle.BackColor = System.Drawing.Color.Transparent
        Me.LblCfgTitle.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.LblCfgTitle.ForeColor = System.Drawing.Color.FromArgb(140, 20, 90)
        Me.LblCfgTitle.Location = New System.Drawing.Point(14, 10)
        Me.LblCfgTitle.Size = New System.Drawing.Size(200, 22)
        Me.LblCfgTitle.Text = "配置文件"

        ' 标题行右侧的类型标签：圆角小胶囊，显示导入文件被识别成什么（只读展示，不参与操作）
        Me.PnlTypeTag.BackColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlTypeTag.FillColor = System.Drawing.Color.FromArgb(250, 228, 240)
        Me.PnlTypeTag.BorderColor = System.Drawing.Color.FromArgb(238, 198, 220)
        Me.PnlTypeTag.Radius = 6
        Me.PnlTypeTag.Location = New System.Drawing.Point(602, 8)
        Me.PnlTypeTag.Size = New System.Drawing.Size(120, 24)

        Me.LblTypeTag.AutoSize = False
        Me.LblTypeTag.BackColor = System.Drawing.Color.Transparent
        Me.LblTypeTag.Font = New System.Drawing.Font("微软雅黑", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.LblTypeTag.ForeColor = System.Drawing.Color.FromArgb(140, 20, 90)
        Me.LblTypeTag.Location = New System.Drawing.Point(0, 0)
        Me.LblTypeTag.Size = New System.Drawing.Size(120, 24)
        Me.LblTypeTag.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        Me.LblTypeTag.Text = "未导入"

        ' 第一行：配置文件（浏览按钮在框内）
        Me.PnlFileShell.BackColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlFileShell.FillColor = System.Drawing.Color.White
        Me.PnlFileShell.BorderColor = System.Drawing.Color.FromArgb(214, 196, 206)
        Me.PnlFileShell.Radius = 6
        Me.PnlFileShell.Location = New System.Drawing.Point(14, 38)
        Me.PnlFileShell.Size = New System.Drawing.Size(708, 32)

        Me.TxtCfgFile.AllowDrop = True
        Me.TxtCfgFile.AutoSize = False
        Me.TxtCfgFile.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtCfgFile.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtCfgFile.Location = New System.Drawing.Point(8, 6)
        Me.TxtCfgFile.Size = New System.Drawing.Size(600, 24)

        Me.BtnCfgBrowse.Surround = System.Drawing.Color.White
        Me.BtnCfgBrowse.BackColor = System.Drawing.Color.FromArgb(237, 232, 236)
        Me.BtnCfgBrowse.Radius = 6
        Me.BtnCfgBrowse.Font = New System.Drawing.Font("微软雅黑", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnCfgBrowse.ForeColor = System.Drawing.Color.FromArgb(140, 20, 90)
        Me.BtnCfgBrowse.Location = New System.Drawing.Point(616, 3)
        Me.BtnCfgBrowse.Size = New System.Drawing.Size(88, 26)
        Me.BtnCfgBrowse.Text = "浏览"

        ' 第二行：导入 KSF（密钥库）
        Me.PnlKsfShell.BackColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlKsfShell.FillColor = System.Drawing.Color.White
        Me.PnlKsfShell.BorderColor = System.Drawing.Color.FromArgb(214, 196, 206)
        Me.PnlKsfShell.Radius = 6
        Me.PnlKsfShell.Location = New System.Drawing.Point(14, 78)
        Me.PnlKsfShell.Size = New System.Drawing.Size(708, 32)

        Me.TxtKmc.AllowDrop = True
        Me.TxtKmc.AutoSize = False
        Me.TxtKmc.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtKmc.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtKmc.Location = New System.Drawing.Point(8, 6)
        Me.TxtKmc.Size = New System.Drawing.Size(600, 24)

        Me.BtnKmcBrowse.Surround = System.Drawing.Color.White
        Me.BtnKmcBrowse.BackColor = System.Drawing.Color.FromArgb(237, 232, 236)
        Me.BtnKmcBrowse.Radius = 6
        Me.BtnKmcBrowse.Font = New System.Drawing.Font("微软雅黑", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnKmcBrowse.ForeColor = System.Drawing.Color.FromArgb(140, 20, 90)
        Me.BtnKmcBrowse.Location = New System.Drawing.Point(616, 3)
        Me.BtnKmcBrowse.Size = New System.Drawing.Size(88, 26)
        Me.BtnKmcBrowse.Text = "浏览"

        ' 第三行：目标格式 + 解密 + 加密。下拉是自绘控件，圆角与输入框一致
        Me.CmbTarget.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.CmbTarget.FillColor = System.Drawing.Color.White
        Me.CmbTarget.BorderColor = System.Drawing.Color.FromArgb(214, 196, 206)
        Me.CmbTarget.ArrowColor = System.Drawing.Color.FromArgb(96, 96, 96)
        Me.CmbTarget.Radius = 6
        Me.CmbTarget.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.CmbTarget.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60)
        Me.CmbTarget.Location = New System.Drawing.Point(14, 118)
        Me.CmbTarget.Size = New System.Drawing.Size(210, 32)

        Me.BtnDecrypt.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnDecrypt.BackColor = System.Drawing.Color.FromArgb(217, 228, 248)
        Me.BtnDecrypt.Radius = 6
        Me.BtnDecrypt.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnDecrypt.ForeColor = System.Drawing.Color.FromArgb(27, 59, 122)
        Me.BtnDecrypt.Location = New System.Drawing.Point(232, 118)
        Me.BtnDecrypt.Size = New System.Drawing.Size(240, 32)
        Me.BtnDecrypt.Text = "解密"

        Me.BtnEncrypt.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnEncrypt.BackColor = System.Drawing.Color.FromArgb(226, 218, 246)
        Me.BtnEncrypt.Radius = 6
        Me.BtnEncrypt.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnEncrypt.ForeColor = System.Drawing.Color.FromArgb(73, 41, 128)
        Me.BtnEncrypt.Location = New System.Drawing.Point(480, 118)
        Me.BtnEncrypt.Size = New System.Drawing.Size(242, 32)
        Me.BtnEncrypt.Text = "加密"

        ' 第四行：下载密钥（仅 $4 下载包需要），一律掩码，没有显示开关
        Me.PnlPassShell.BackColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlPassShell.FillColor = System.Drawing.Color.White
        Me.PnlPassShell.BorderColor = System.Drawing.Color.FromArgb(214, 196, 206)
        Me.PnlPassShell.Radius = 6
        Me.PnlPassShell.Location = New System.Drawing.Point(14, 158)
        Me.PnlPassShell.Size = New System.Drawing.Size(708, 32)

        Me.TxtPass.AllowDrop = False
        Me.TxtPass.AutoSize = False
        Me.TxtPass.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtPass.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtPass.Location = New System.Drawing.Point(8, 6)
        Me.TxtPass.Size = New System.Drawing.Size(692, 24)

        Me.TxtPass.UseSystemPasswordChar = True

        Me.PnlCfg.Controls.Add(Me.LblCfgTitle)
        Me.PnlCfg.Controls.Add(Me.PnlTypeTag)
        Me.PnlTypeTag.Controls.Add(Me.LblTypeTag)
        Me.PnlCfg.Controls.Add(Me.PnlFileShell)
        Me.PnlCfg.Controls.Add(Me.PnlKsfShell)
        Me.PnlCfg.Controls.Add(Me.CmbTarget)
        Me.PnlCfg.Controls.Add(Me.BtnDecrypt)
        Me.PnlCfg.Controls.Add(Me.BtnEncrypt)
        Me.PnlCfg.Controls.Add(Me.PnlPassShell)
        Me.PnlFileShell.Controls.Add(Me.TxtCfgFile)
        Me.PnlFileShell.Controls.Add(Me.BtnCfgBrowse)
        Me.PnlKsfShell.Controls.Add(Me.TxtKmc)
        Me.PnlKsfShell.Controls.Add(Me.BtnKmcBrowse)
        Me.PnlPassShell.Controls.Add(Me.TxtPass)

        ' ================= 卡片二：密文字段 =================
        Me.PnlField.BackColor = System.Drawing.Color.MediumVioletRed
        Me.PnlField.FillColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlField.BorderColor = System.Drawing.Color.Empty
        Me.PnlField.Radius = 6
        Me.PnlField.Location = New System.Drawing.Point(12, 278)
        Me.PnlField.Size = New System.Drawing.Size(736, 208)

        Me.LblFieldTitle.AutoSize = False
        Me.LblFieldTitle.BackColor = System.Drawing.Color.Transparent
        Me.LblFieldTitle.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.LblFieldTitle.ForeColor = System.Drawing.Color.FromArgb(140, 20, 90)
        Me.LblFieldTitle.Location = New System.Drawing.Point(14, 10)
        Me.LblFieldTitle.Size = New System.Drawing.Size(200, 22)
        Me.LblFieldTitle.Text = "密文字段"

        ' 输入行
        Me.PnlFieldInShell.BackColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlFieldInShell.FillColor = System.Drawing.Color.White
        Me.PnlFieldInShell.BorderColor = System.Drawing.Color.FromArgb(214, 196, 206)
        Me.PnlFieldInShell.Radius = 6
        Me.PnlFieldInShell.Location = New System.Drawing.Point(14, 38)
        Me.PnlFieldInShell.Size = New System.Drawing.Size(708, 32)

        Me.TxtFieldIn.AllowDrop = True
        Me.TxtFieldIn.AutoSize = False
        Me.TxtFieldIn.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtFieldIn.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtFieldIn.Location = New System.Drawing.Point(8, 6)
        Me.TxtFieldIn.Size = New System.Drawing.Size(692, 24)

        ' 输出行：复制按钮在框内；ReadOnly 会被系统画成灰底，必须显式给白色
        Me.PnlFieldOutShell.BackColor = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.PnlFieldOutShell.FillColor = System.Drawing.Color.White
        Me.PnlFieldOutShell.BorderColor = System.Drawing.Color.FromArgb(214, 196, 206)
        Me.PnlFieldOutShell.Radius = 6
        Me.PnlFieldOutShell.Location = New System.Drawing.Point(14, 78)
        Me.PnlFieldOutShell.Size = New System.Drawing.Size(708, 32)

        Me.TxtFieldOut.AllowDrop = False
        Me.TxtFieldOut.AutoSize = False
        Me.TxtFieldOut.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtFieldOut.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtFieldOut.Location = New System.Drawing.Point(8, 6)
        Me.TxtFieldOut.Size = New System.Drawing.Size(600, 24)

        Me.TxtFieldOut.BackColor = System.Drawing.Color.White
        Me.TxtFieldOut.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60)
        Me.TxtFieldOut.ReadOnly = True
        Me.BtnCopyField.Surround = System.Drawing.Color.White
        Me.BtnCopyField.BackColor = System.Drawing.Color.FromArgb(237, 232, 236)
        Me.BtnCopyField.Radius = 6
        Me.BtnCopyField.Font = New System.Drawing.Font("微软雅黑", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnCopyField.ForeColor = System.Drawing.Color.FromArgb(140, 20, 90)
        Me.BtnCopyField.Location = New System.Drawing.Point(616, 3)
        Me.BtnCopyField.Size = New System.Drawing.Size(88, 26)
        Me.BtnCopyField.Text = "复制"

        Me.BtnFieldAuto.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnFieldAuto.BackColor = System.Drawing.Color.FromArgb(207, 235, 230)
        Me.BtnFieldAuto.Radius = 6
        Me.BtnFieldAuto.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnFieldAuto.ForeColor = System.Drawing.Color.FromArgb(16, 83, 74)
        Me.BtnFieldAuto.Location = New System.Drawing.Point(14, 122)
        Me.BtnFieldAuto.Size = New System.Drawing.Size(230, 32)
        Me.BtnFieldAuto.Text = "自适应解密"

        Me.BtnFieldD2B.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnFieldD2B.BackColor = System.Drawing.Color.FromArgb(216, 238, 203)
        Me.BtnFieldD2B.Radius = 6
        Me.BtnFieldD2B.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnFieldD2B.ForeColor = System.Drawing.Color.FromArgb(42, 90, 30)
        Me.BtnFieldD2B.Location = New System.Drawing.Point(253, 122)
        Me.BtnFieldD2B.Size = New System.Drawing.Size(230, 32)
        Me.BtnFieldD2B.Text = "$2 BOARDINFO 解密"

        Me.BtnFieldD2O.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnFieldD2O.BackColor = System.Drawing.Color.FromArgb(251, 227, 207)
        Me.BtnFieldD2O.Radius = 6
        Me.BtnFieldD2O.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnFieldD2O.ForeColor = System.Drawing.Color.FromArgb(123, 67, 16)
        Me.BtnFieldD2O.Location = New System.Drawing.Point(492, 122)
        Me.BtnFieldD2O.Size = New System.Drawing.Size(230, 32)
        Me.BtnFieldD2O.Text = "$2 OMCI 解密"

        Me.BtnFieldE1.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnFieldE1.BackColor = System.Drawing.Color.FromArgb(249, 216, 226)
        Me.BtnFieldE1.Radius = 6
        Me.BtnFieldE1.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnFieldE1.ForeColor = System.Drawing.Color.FromArgb(138, 29, 62)
        Me.BtnFieldE1.Location = New System.Drawing.Point(14, 162)
        Me.BtnFieldE1.Size = New System.Drawing.Size(230, 32)
        Me.BtnFieldE1.Text = "$1 加密"

        Me.BtnFieldE2.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnFieldE2.BackColor = System.Drawing.Color.FromArgb(250, 237, 196)
        Me.BtnFieldE2.Radius = 6
        Me.BtnFieldE2.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnFieldE2.ForeColor = System.Drawing.Color.FromArgb(107, 85, 0)
        Me.BtnFieldE2.Location = New System.Drawing.Point(253, 162)
        Me.BtnFieldE2.Size = New System.Drawing.Size(230, 32)
        Me.BtnFieldE2.Text = "$2 加密"

        Me.BtnFieldE4.Surround = System.Drawing.Color.FromArgb(255, 243, 249)
        Me.BtnFieldE4.BackColor = System.Drawing.Color.FromArgb(219, 224, 239)
        Me.BtnFieldE4.Radius = 6
        Me.BtnFieldE4.Font = New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.BtnFieldE4.ForeColor = System.Drawing.Color.FromArgb(48, 59, 94)
        Me.BtnFieldE4.Location = New System.Drawing.Point(492, 162)
        Me.BtnFieldE4.Size = New System.Drawing.Size(230, 32)
        Me.BtnFieldE4.Text = "$4 加密"

        Me.PnlField.Controls.Add(Me.LblFieldTitle)
        Me.PnlField.Controls.Add(Me.PnlFieldInShell)
        Me.PnlField.Controls.Add(Me.PnlFieldOutShell)
        Me.PnlField.Controls.Add(Me.BtnFieldAuto)
        Me.PnlField.Controls.Add(Me.BtnFieldD2B)
        Me.PnlField.Controls.Add(Me.BtnFieldD2O)
        Me.PnlField.Controls.Add(Me.BtnFieldE1)
        Me.PnlField.Controls.Add(Me.BtnFieldE2)
        Me.PnlField.Controls.Add(Me.BtnFieldE4)
        Me.PnlFieldInShell.Controls.Add(Me.TxtFieldIn)
        Me.PnlFieldOutShell.Controls.Add(Me.TxtFieldOut)
        Me.PnlFieldOutShell.Controls.Add(Me.BtnCopyField)

        ' ================= 底部提示行 =================
        ' 只读但可选、可 Ctrl+C —— 报错信息能直接选中复制去搜，比弹窗里抄字省事。
        ' 只读 TextBox 会被系统画成灰底，必须显式给白色。
        ' 文字左缩进 22 = 卡片内边距 14 + 输入行内缩 8，和上面几行文字严格对齐。
        Me.PnlMsgShell.BackColor = System.Drawing.Color.MediumVioletRed
        Me.PnlMsgShell.FillColor = System.Drawing.Color.White
        Me.PnlMsgShell.BorderColor = System.Drawing.Color.FromArgb(214, 196, 206)
        Me.PnlMsgShell.Radius = 6
        Me.PnlMsgShell.Location = New System.Drawing.Point(12, 498)
        Me.PnlMsgShell.Size = New System.Drawing.Size(736, 32)

        Me.TxtMsg.AllowDrop = False
        Me.TxtMsg.AutoSize = False
        Me.TxtMsg.BackColor = System.Drawing.Color.White
        Me.TxtMsg.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.TxtMsg.Font = New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(134, Byte))
        Me.TxtMsg.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60)
        Me.TxtMsg.Location = New System.Drawing.Point(22, 6)
        Me.TxtMsg.ReadOnly = True
        Me.TxtMsg.Size = New System.Drawing.Size(692, 24)
        Me.TxtMsg.TabStop = False

        Me.PnlMsgShell.Controls.Add(Me.TxtMsg)

        ' ---- 窗体：洋红满铺，无状态栏 ----
        ' 进程已在 app.manifest 里声明 DPI 感知，这里用 Dpi 自动缩放：
        ' 96dpi 下按设计稿 760x542，125% 时整体放大，字号由 GDI 按物理 DPI 排版，不糊。
        Me.AllowDrop = True
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.MediumVioletRed
        Me.ClientSize = New System.Drawing.Size(760, 542)
        Me.Controls.Add(Me.LblTitle)
        Me.Controls.Add(Me.PnlCfg)
        Me.Controls.Add(Me.PnlField)
        Me.Controls.Add(Me.PnlMsgShell)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "MiniForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "HW Dollar4 mini"

        Me.PnlFieldOutShell.ResumeLayout(False)
        Me.PnlFieldInShell.ResumeLayout(False)
        Me.PnlPassShell.ResumeLayout(False)
        Me.PnlKsfShell.ResumeLayout(False)
        Me.PnlFileShell.ResumeLayout(False)
        Me.PnlMsgShell.ResumeLayout(False)
        Me.PnlTypeTag.ResumeLayout(False)
        Me.PnlField.ResumeLayout(False)
        Me.PnlCfg.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LblTitle As System.Windows.Forms.Label
    Friend WithEvents PnlCfg As RoundBox
    Friend WithEvents LblCfgTitle As System.Windows.Forms.Label
    Friend WithEvents PnlTypeTag As RoundBox
    Friend WithEvents LblTypeTag As System.Windows.Forms.Label
    Friend WithEvents PnlFileShell As RoundBox
    Friend WithEvents PnlKsfShell As RoundBox
    Friend WithEvents PnlPassShell As RoundBox
    Friend WithEvents TxtCfgFile As System.Windows.Forms.TextBox
    Friend WithEvents BtnCfgBrowse As RoundButton
    Friend WithEvents TxtKmc As System.Windows.Forms.TextBox
    Friend WithEvents BtnKmcBrowse As RoundButton
    Friend WithEvents CmbTarget As RoundCombo
    Friend WithEvents BtnDecrypt As RoundButton
    Friend WithEvents BtnEncrypt As RoundButton
    Friend WithEvents TxtPass As System.Windows.Forms.TextBox
    Friend WithEvents PnlField As RoundBox
    Friend WithEvents LblFieldTitle As System.Windows.Forms.Label
    Friend WithEvents PnlFieldInShell As RoundBox
    Friend WithEvents PnlFieldOutShell As RoundBox
    Friend WithEvents TxtFieldIn As System.Windows.Forms.TextBox
    Friend WithEvents TxtFieldOut As System.Windows.Forms.TextBox
    Friend WithEvents BtnCopyField As RoundButton
    Friend WithEvents BtnFieldAuto As RoundButton
    Friend WithEvents BtnFieldD2B As RoundButton
    Friend WithEvents BtnFieldD2O As RoundButton
    Friend WithEvents BtnFieldE1 As RoundButton
    Friend WithEvents BtnFieldE2 As RoundButton
    Friend WithEvents BtnFieldE4 As RoundButton
    Friend WithEvents PnlMsgShell As RoundBox
    Friend WithEvents TxtMsg As System.Windows.Forms.TextBox
End Class