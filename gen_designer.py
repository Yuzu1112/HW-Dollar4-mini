# -*- coding: utf-8 -*-
"""把 Mini 的布局数据展开成 VS 设计器能解析的标准 InitializeComponent。

设计器只认「Me.X = New T()」「Me.X.Prop = 常量表达式」这类语句，
不接受局部变量、方法调用、循环、If —— 所以布局必须显式写出来。
这个脚本就是那张布局表的唯一来源，改布局改这里再跑一次。
"""
import io

OUT = r'C:\Users\QWQ\Documents\Code\HW-Dollar4-Mini\MiniForm.Designer.vb'

F_UI = ('New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Regular, '
        'System.Drawing.GraphicsUnit.Point, CType(134, Byte))')
F_BTN = ('New System.Drawing.Font("微软雅黑", 11.0!, System.Drawing.FontStyle.Bold, '
         'System.Drawing.GraphicsUnit.Point, CType(134, Byte))')
F_SEC = ('New System.Drawing.Font("微软雅黑", 10.5!, System.Drawing.FontStyle.Bold, '
         'System.Drawing.GraphicsUnit.Point, CType(134, Byte))')
F_INNER = ('New System.Drawing.Font("微软雅黑", 10.0!, System.Drawing.FontStyle.Regular, '
           'System.Drawing.GraphicsUnit.Point, CType(134, Byte))')
F_TAG = ('New System.Drawing.Font("微软雅黑", 9.0!, System.Drawing.FontStyle.Regular, '
         'System.Drawing.GraphicsUnit.Point, CType(134, Byte))')


def rgb(r, g, b):
    return 'System.Drawing.Color.FromArgb({0}, {1}, {2})'.format(r, g, b)


C_CARD = rgb(255, 243, 249)
C_INNER_BTN = rgb(237, 232, 236)
C_SEC = rgb(140, 20, 90)
C_BORDER = rgb(214, 196, 206)
C_BODY = rgb(60, 60, 60)
C_WHITE = 'System.Drawing.Color.White'
C_TRANS = 'System.Drawing.Color.Transparent'
C_MAGENTA = 'System.Drawing.Color.MediumVioletRed'
C_TAG_FILL = rgb(250, 228, 240)   # 类型标签底色：卡片粉底上再压一档，和白色输入行区分得开
C_TAG_LINE = rgb(238, 198, 220)   # 标签描边：纯色底在浅粉卡片上太淡，加一档才看得出边界

# 八个主按钮各一种浅色底 + 同色系深色字（浅底配白字看不清，必须换成深字）
# 括号里是取色思路：底 220~250 亮度段，字压到 60 以下，对比度都在 7:1 以上
C_BTN_BLUE = (rgb(217, 228, 248), rgb(27, 59, 122))    # 浅蓝 / 深蓝
C_BTN_PURPLE = (rgb(226, 218, 246), rgb(73, 41, 128))  # 浅紫 / 深紫
C_BTN_TEAL = (rgb(207, 235, 230), rgb(16, 83, 74))     # 浅青 / 墨青
C_BTN_GREEN = (rgb(216, 238, 203), rgb(42, 90, 30))    # 浅绿 / 深绿
C_BTN_ORANGE = (rgb(251, 227, 207), rgb(123, 67, 16))  # 浅橙 / 深棕
C_BTN_ROSE = (rgb(249, 216, 226), rgb(138, 29, 62))    # 浅玫瑰 / 深红
C_BTN_SAND = (rgb(250, 237, 196), rgb(107, 85, 0))     # 浅砂 / 深黄褐
C_BTN_SLATE = (rgb(219, 224, 239), rgb(48, 59, 94))    # 浅灰蓝 / 深靛

FIELD_H = 32
INNER_W = 708          # 卡片内宽：736 - 14*2
shells = []            # (name, x, y, w)
textboxes = []         # (name, shell, w, reserve, allowdrop)
inner_buttons = []     # (name, shell, text)
buttons = []           # (name, parent, x, y, w, h, text, back, fore)


# ---- 卡片一：配置文件 ----
shells += [('PnlFileShell', 14, 38, INNER_W), ('PnlKsfShell', 14, 78, INNER_W),
           ('PnlPassShell', 14, 158, INNER_W)]
textboxes += [('TxtCfgFile', 'PnlFileShell', 92, True),
              ('TxtKmc', 'PnlKsfShell', 92, True),
              ('TxtPass', 'PnlPassShell', 0, False)]
inner_buttons += [('BtnCfgBrowse', 'PnlFileShell', '浏览'),
                  ('BtnKmcBrowse', 'PnlKsfShell', '浏览')]
buttons += [('BtnDecrypt', 'PnlCfg', 232, 118, 240, FIELD_H, '解密', C_BTN_BLUE[0], C_BTN_BLUE[1]),
            ('BtnEncrypt', 'PnlCfg', 480, 118, 242, FIELD_H, '加密', C_BTN_PURPLE[0], C_BTN_PURPLE[1])]

# ---- 底部提示行：单行只读文本框，压在洋红底上，和输入行同一套圆角外壳 ----
MSG_Y = 498
MSG_H = FIELD_H

# ---- 卡片二：密文字段 ----
shells += [('PnlFieldInShell', 14, 38, INNER_W), ('PnlFieldOutShell', 14, 78, INNER_W)]
textboxes += [('TxtFieldIn', 'PnlFieldInShell', 0, True),
              ('TxtFieldOut', 'PnlFieldOutShell', 92, False)]
inner_buttons += [('BtnCopyField', 'PnlFieldOutShell', '复制')]
buttons += [('BtnFieldAuto', 'PnlField', 14, 122, 230, FIELD_H, '自适应解密', C_BTN_TEAL[0], C_BTN_TEAL[1]),
            ('BtnFieldD2B', 'PnlField', 253, 122, 230, FIELD_H, '$2 BOARDINFO 解密', C_BTN_GREEN[0], C_BTN_GREEN[1]),
            ('BtnFieldD2O', 'PnlField', 492, 122, 230, FIELD_H, '$2 OMCI 解密', C_BTN_ORANGE[0], C_BTN_ORANGE[1]),
            ('BtnFieldE1', 'PnlField', 14, 162, 230, FIELD_H, '$1 加密', C_BTN_ROSE[0], C_BTN_ROSE[1]),
            ('BtnFieldE2', 'PnlField', 253, 162, 230, FIELD_H, '$2 加密', C_BTN_SAND[0], C_BTN_SAND[1]),
            ('BtnFieldE4', 'PnlField', 492, 162, 230, FIELD_H, '$4 加密', C_BTN_SLATE[0], C_BTN_SLATE[1])]

TARGET_ITEMS = ['02 容器（$2）', '01 容器（$1）', '03 容器（$4）', '下载包（02）',
                '下载包（03）', 'boardinfo V5', 'boardinfo V6', '裸块（$4）']


def pt(x, y):
    return 'New System.Drawing.Point({0}, {1})'.format(x, y)


def sz(w, h):
    return 'New System.Drawing.Size({0}, {1})'.format(w, h)


L = []
a = L.append

a("' HW Dollar4 Mini 主窗体 —— 控件构建部分。")
a("'")
a("' 这个文件必须保持「设计器可解析」的标准格式：")
a("'   只允许 Me.X = New T() 和 Me.X.属性 = 常量表达式，")
a("'   不能写局部变量、辅助方法调用、循环、If —— 否则 VS 打开设计器会报")
a("'   「未能加载基类 System.Windows.Forms.Form」。布局数据由 gen_designer.py 生成。")
a("'")
a("' 两张功能卡片：洋红铺满窗体背景，卡片用浅粉白（RoundBox 4px 圆角）；")
a("' 每行输入是一个 RoundBox 外壳（高 32），里面装无边框 TextBox 和右侧按钮，")
a("' 于是按钮视觉上在输入框内部，且所有行高严格一致。")
a("<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _")
a("Partial Class MiniForm")
a("    Inherits System.Windows.Forms.Form")
a("")
a("    ' 窗体重写 Dispose，清理组件列表")
a("    <System.Diagnostics.DebuggerNonUserCode()> _")
a("    Protected Overrides Sub Dispose(disposing As Boolean)")
a("        Try")
a("            If disposing AndAlso components IsNot Nothing Then")
a("                components.Dispose()")
a("            End If")
a("        Finally")
a("            MyBase.Dispose(disposing)")
a("        End Try")
a("    End Sub")
a("")
a("    Private components As System.ComponentModel.IContainer")
a("")
a("    Public Sub New()")
a("        MyBase.New()")
a("        ' 设计器要求：构造里必须调用 InitializeComponent，")
a("        ' 少了它窗体是空的（这条在 VB 模板里由设计器自动写）")
a("        InitializeComponent()")
a("    End Sub")
a("")
a("    ' 注意：本方法由设计器与 gen_designer.py 共同维护，请勿手工插入逻辑")
a("    <System.Diagnostics.DebuggerStepThrough()> _")
a("    Private Sub InitializeComponent()")

# 声明
decl = ['LblTitle', 'PnlCfg', 'LblCfgTitle', 'PnlTypeTag', 'LblTypeTag'] + [s[0] for s in shells[:3]] + \
       ['TxtCfgFile', 'BtnCfgBrowse', 'TxtKmc', 'BtnKmcBrowse', 'CmbTarget',
        'BtnDecrypt', 'BtnEncrypt', 'TxtPass',
        'PnlField', 'LblFieldTitle'] + [s[0] for s in shells[3:]] + \
       ['TxtFieldIn', 'TxtFieldOut', 'BtnCopyField',
        'BtnFieldAuto', 'BtnFieldD2B', 'BtnFieldD2O', 'BtnFieldE1', 'BtnFieldE2', 'BtnFieldE4',
        'PnlMsgShell', 'TxtMsg']

types = {}
types['LblTitle'] = 'System.Windows.Forms.Label'
types['PnlCfg'] = 'RoundBox'
types['LblCfgTitle'] = 'System.Windows.Forms.Label'
types['PnlField'] = 'RoundBox'
types['LblFieldTitle'] = 'System.Windows.Forms.Label'
types['PnlTypeTag'] = 'RoundBox'
types['LblTypeTag'] = 'System.Windows.Forms.Label'
types['PnlMsgShell'] = 'RoundBox'
types['TxtMsg'] = 'System.Windows.Forms.TextBox'
for s in shells:
    types[s[0]] = 'RoundBox'
for t in textboxes:
    types[t[0]] = 'System.Windows.Forms.TextBox'
for b in inner_buttons:
    types[b[0]] = 'RoundButton'
for b in buttons:
    types[b[0]] = 'RoundButton'
types['CmbTarget'] = 'RoundCombo'

for n in decl:
    a("        Me.{0} = New {1}()".format(n, types[n]))

# SuspendLayout
for n in ('PnlCfg', 'PnlField', 'PnlMsgShell', 'PnlTypeTag'):
    a("        Me.{0}.SuspendLayout()".format(n))
for s in shells:
    a("        Me.{0}.SuspendLayout()".format(s[0]))
a("        Me.SuspendLayout()")
a("")

# ---- 标题 ----
a("        ' ---- 标题：直接压在洋红底上，居中 ----")
a("        Me.LblTitle.AutoSize = False")
a("        Me.LblTitle.BackColor = {0}".format(C_TRANS))
a("        Me.LblTitle.Font = {0}".format(F_BTN.replace('11.0!, System.Drawing.FontStyle.Bold', '12.0!, System.Drawing.FontStyle.Bold')))
a("        Me.LblTitle.ForeColor = {0}".format(C_WHITE))
a("        Me.LblTitle.Location = {0}".format(pt(0, 14)))
a("        Me.LblTitle.Size = {0}".format(sz(760, 34)))
a("        Me.LblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter")
a("        Me.LblTitle.Text = \"HW Dollar4 Mini 版本: 4.42.0 (2026.08)\"")
a("")

# ---- 卡片一 ----
a("        ' ================= 卡片一：配置文件 =================")
a("        Me.PnlCfg.BackColor = {0}".format(C_MAGENTA))
a("        Me.PnlCfg.FillColor = {0}".format(C_CARD))
a("        Me.PnlCfg.BorderColor = System.Drawing.Color.Empty")
a("        Me.PnlCfg.Radius = 6")
a("        Me.PnlCfg.Location = {0}".format(pt(12, 62)))
a("        Me.PnlCfg.Size = {0}".format(sz(736, 204)))
a("")
a("        Me.LblCfgTitle.AutoSize = False")
a("        Me.LblCfgTitle.BackColor = {0}".format(C_TRANS))
a("        Me.LblCfgTitle.Font = {0}".format(F_SEC))
a("        Me.LblCfgTitle.ForeColor = {0}".format(C_SEC))
a("        Me.LblCfgTitle.Location = {0}".format(pt(14, 10)))
a("        Me.LblCfgTitle.Size = {0}".format(sz(200, 22)))
a("        Me.LblCfgTitle.Text = \"配置文件\"")
a("")
a("        ' 标题行右侧的类型标签：圆角小胶囊，显示导入文件被识别成什么（只读展示，不参与操作）")
a("        Me.PnlTypeTag.BackColor = {0}".format(C_CARD))
a("        Me.PnlTypeTag.FillColor = {0}".format(C_TAG_FILL))
a("        Me.PnlTypeTag.BorderColor = {0}".format(C_TAG_LINE))
a("        Me.PnlTypeTag.Radius = 6")
a("        Me.PnlTypeTag.Location = {0}".format(pt(602, 8)))
a("        Me.PnlTypeTag.Size = {0}".format(sz(120, 24)))
a("")
a("        Me.LblTypeTag.AutoSize = False")
a("        Me.LblTypeTag.BackColor = {0}".format(C_TRANS))
a("        Me.LblTypeTag.Font = {0}".format(F_TAG))
a("        Me.LblTypeTag.ForeColor = {0}".format(C_SEC))
a("        Me.LblTypeTag.Location = {0}".format(pt(0, 0)))
a("        Me.LblTypeTag.Size = {0}".format(sz(120, 24)))
a("        Me.LblTypeTag.TextAlign = System.Drawing.ContentAlignment.MiddleCenter")
a("        Me.LblTypeTag.Text = \"未导入\"")
a("")


def emit_shell(name, x, y, w):
    a("        Me.{0}.BackColor = {1}".format(name, C_CARD))
    a("        Me.{0}.FillColor = {1}".format(name, C_WHITE))
    a("        Me.{0}.BorderColor = {1}".format(name, C_BORDER))
    a("        Me.{0}.Radius = 6".format(name))
    a("        Me.{0}.Location = {1}".format(name, pt(x, y)))
    a("        Me.{0}.Size = {1}".format(name, sz(w, FIELD_H)))
    a("")


def emit_textbox(name, shell, width, reserve, allowdrop):
    """单行 TextBox 的 AutoSize=True 会把高度锁死在字体行高（10.5pt 时 19px），
    下划线落在字形底部以下就被客户区裁掉，表现成「下划线不显示」。
    必须关掉 AutoSize 并显式给高。

    垂直位置：EDIT 把文本垂直居中于客户区，高度给 28 时居中偏移 4.5px，
    字形在行高里偏上，实测文字比视觉中心高 3px。改成高 24、y=5，
    居中偏移降到 2.5px，并整体下移，留白才接近上下对称。"""
    a("        Me.{0}.AllowDrop = {1}".format(name, 'True' if allowdrop else 'False'))
    a("        Me.{0}.AutoSize = False".format(name))
    a("        Me.{0}.BorderStyle = System.Windows.Forms.BorderStyle.None".format(name))
    a("        Me.{0}.Font = {1}".format(name, F_UI))
    a("        Me.{0}.Location = {1}".format(name, pt(8, 6)))
    a("        Me.{0}.Size = {1}".format(name, sz(width - 16 - reserve, FIELD_H - 8)))
    a("")


def emit_inner(name, shell, text):
    a("        Me.{0}.Surround = {1}".format(name, C_WHITE))
    a("        Me.{0}.BackColor = {1}".format(name, C_INNER_BTN))
    a("        Me.{0}.Radius = 6".format(name))
    a("        Me.{0}.Font = {1}".format(name, F_INNER))
    a("        Me.{0}.ForeColor = {1}".format(name, C_SEC))
    a("        Me.{0}.Location = {1}".format(name, pt(INNER_W - 92, 3)))
    a("        Me.{0}.Size = {1}".format(name, sz(88, FIELD_H - 6)))
    a("        Me.{0}.Text = \"{1}\"".format(name, text))
    a("")


def emit_button(name, parent, x, y, w, h, text, back, fore):
    a("        Me.{0}.Surround = {1}".format(name, C_CARD))
    a("        Me.{0}.BackColor = {1}".format(name, back))
    a("        Me.{0}.Radius = 6".format(name))
    a("        Me.{0}.Font = {1}".format(name, F_BTN))
    a("        Me.{0}.ForeColor = {1}".format(name, fore))
    a("        Me.{0}.Location = {1}".format(name, pt(x, y)))
    a("        Me.{0}.Size = {1}".format(name, sz(w, h)))
    a("        Me.{0}.Text = \"{1}\"".format(name, text))
    a("")


a("        ' 第一行：配置文件（浏览按钮在框内）")
emit_shell('PnlFileShell', 14, 38, INNER_W)
emit_textbox('TxtCfgFile', 'PnlFileShell', INNER_W, 92, True)
emit_inner('BtnCfgBrowse', 'PnlFileShell', '浏览')

a("        ' 第二行：导入 KSF（密钥库）")
emit_shell('PnlKsfShell', 14, 78, INNER_W)
emit_textbox('TxtKmc', 'PnlKsfShell', INNER_W, 92, True)
emit_inner('BtnKmcBrowse', 'PnlKsfShell', '浏览')

a("        ' 第三行：目标格式 + 解密 + 加密。下拉是自绘控件，圆角与输入框一致")
a("        Me.CmbTarget.Surround = {0}".format(C_CARD))
a("        Me.CmbTarget.FillColor = {0}".format(C_WHITE))
a("        Me.CmbTarget.BorderColor = {0}".format(C_BORDER))
a("        Me.CmbTarget.ArrowColor = {0}".format(rgb(96, 96, 96)))
a("        Me.CmbTarget.Radius = 6")
a("        Me.CmbTarget.Font = {0}".format(F_UI))
a("        Me.CmbTarget.ForeColor = {0}".format(C_BODY))
a("        Me.CmbTarget.Location = {0}".format(pt(14, 118)))
a("        Me.CmbTarget.Size = {0}".format(sz(210, FIELD_H)))
a("")
emit_button(*buttons[0])
emit_button(*buttons[1])

a("        ' 第四行：下载密钥（仅 $4 下载包需要），一律掩码，没有显示开关")
emit_shell('PnlPassShell', 14, 158, INNER_W)
emit_textbox('TxtPass', 'PnlPassShell', INNER_W, 0, False)
a("        Me.TxtPass.UseSystemPasswordChar = True")
a("")

a("        Me.PnlCfg.Controls.Add(Me.LblCfgTitle)")
a("        Me.PnlCfg.Controls.Add(Me.PnlTypeTag)")
a("        Me.PnlTypeTag.Controls.Add(Me.LblTypeTag)")
a("        Me.PnlCfg.Controls.Add(Me.PnlFileShell)")
a("        Me.PnlCfg.Controls.Add(Me.PnlKsfShell)")
a("        Me.PnlCfg.Controls.Add(Me.CmbTarget)")
a("        Me.PnlCfg.Controls.Add(Me.BtnDecrypt)")
a("        Me.PnlCfg.Controls.Add(Me.BtnEncrypt)")
a("        Me.PnlCfg.Controls.Add(Me.PnlPassShell)")
a("        Me.PnlFileShell.Controls.Add(Me.TxtCfgFile)")
a("        Me.PnlFileShell.Controls.Add(Me.BtnCfgBrowse)")
a("        Me.PnlKsfShell.Controls.Add(Me.TxtKmc)")
a("        Me.PnlKsfShell.Controls.Add(Me.BtnKmcBrowse)")
a("        Me.PnlPassShell.Controls.Add(Me.TxtPass)")
a("")

# ---- 卡片二 ----
a("        ' ================= 卡片二：密文字段 =================")
a("        Me.PnlField.BackColor = {0}".format(C_MAGENTA))
a("        Me.PnlField.FillColor = {0}".format(C_CARD))
a("        Me.PnlField.BorderColor = System.Drawing.Color.Empty")
a("        Me.PnlField.Radius = 6")
a("        Me.PnlField.Location = {0}".format(pt(12, 278)))
a("        Me.PnlField.Size = {0}".format(sz(736, 208)))
a("")
a("        Me.LblFieldTitle.AutoSize = False")
a("        Me.LblFieldTitle.BackColor = {0}".format(C_TRANS))
a("        Me.LblFieldTitle.Font = {0}".format(F_SEC))
a("        Me.LblFieldTitle.ForeColor = {0}".format(C_SEC))
a("        Me.LblFieldTitle.Location = {0}".format(pt(14, 10)))
a("        Me.LblFieldTitle.Size = {0}".format(sz(200, 22)))
a("        Me.LblFieldTitle.Text = \"密文字段\"")
a("")

a("        ' 输入行")
emit_shell('PnlFieldInShell', 14, 38, INNER_W)
emit_textbox('TxtFieldIn', 'PnlFieldInShell', INNER_W, 0, True)

a("        ' 输出行：复制按钮在框内；ReadOnly 会被系统画成灰底，必须显式给白色")
emit_shell('PnlFieldOutShell', 14, 78, INNER_W)
emit_textbox('TxtFieldOut', 'PnlFieldOutShell', INNER_W, 92, False)
a("        Me.TxtFieldOut.BackColor = {0}".format(C_WHITE))
a("        Me.TxtFieldOut.ForeColor = {0}".format(C_BODY))
a("        Me.TxtFieldOut.ReadOnly = True")
emit_inner('BtnCopyField', 'PnlFieldOutShell', '复制')

for b in buttons[2:]:
    emit_button(*b)

a("        Me.PnlField.Controls.Add(Me.LblFieldTitle)")
a("        Me.PnlField.Controls.Add(Me.PnlFieldInShell)")
a("        Me.PnlField.Controls.Add(Me.PnlFieldOutShell)")
for b in buttons[2:]:
    a("        Me.PnlField.Controls.Add(Me.{0})".format(b[0]))
a("        Me.PnlFieldInShell.Controls.Add(Me.TxtFieldIn)")
a("        Me.PnlFieldOutShell.Controls.Add(Me.TxtFieldOut)")
a("        Me.PnlFieldOutShell.Controls.Add(Me.BtnCopyField)")
a("")

# ---- 底部提示行 ----
a("        ' ================= 底部提示行 =================")
a("        ' 只读但可选、可 Ctrl+C —— 报错信息能直接选中复制去搜，比弹窗里抄字省事。")
a("        ' 只读 TextBox 会被系统画成灰底，必须显式给白色。")
a("        ' 文字左缩进 22 = 卡片内边距 14 + 输入行内缩 8，和上面几行文字严格对齐。")
a("        Me.PnlMsgShell.BackColor = {0}".format(C_MAGENTA))
a("        Me.PnlMsgShell.FillColor = {0}".format(C_WHITE))
a("        Me.PnlMsgShell.BorderColor = {0}".format(C_BORDER))
a("        Me.PnlMsgShell.Radius = 6")
a("        Me.PnlMsgShell.Location = {0}".format(pt(12, MSG_Y)))
a("        Me.PnlMsgShell.Size = {0}".format(sz(736, MSG_H)))
a("")
a("        Me.TxtMsg.AllowDrop = False")
a("        Me.TxtMsg.AutoSize = False")
a("        Me.TxtMsg.BackColor = {0}".format(C_WHITE))
a("        Me.TxtMsg.BorderStyle = System.Windows.Forms.BorderStyle.None")
a("        Me.TxtMsg.Font = {0}".format(F_UI))
a("        Me.TxtMsg.ForeColor = {0}".format(C_BODY))
a("        Me.TxtMsg.Location = {0}".format(pt(22, 6)))
a("        Me.TxtMsg.ReadOnly = True")
a("        Me.TxtMsg.Size = {0}".format(sz(736 - 44, MSG_H - 8)))
a("        Me.TxtMsg.TabStop = False")
a("")
a("        Me.PnlMsgShell.Controls.Add(Me.TxtMsg)")
a("")

# ---- 窗体 ----
a("        ' ---- 窗体：洋红满铺，无状态栏 ----")
a("        ' 进程已在 app.manifest 里声明 DPI 感知，这里用 Dpi 自动缩放：")
a("        ' 96dpi 下按设计稿 760x542，125% 时整体放大，字号由 GDI 按物理 DPI 排版，不糊。")
a("        Me.AllowDrop = True")
a("        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)")
a("        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi")
a("        Me.BackColor = {0}".format(C_MAGENTA))
a("        Me.ClientSize = {0}".format(sz(760, 542)))
a("        Me.Controls.Add(Me.LblTitle)")
a("        Me.Controls.Add(Me.PnlCfg)")
a("        Me.Controls.Add(Me.PnlField)")
a("        Me.Controls.Add(Me.PnlMsgShell)")
a("        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle")
a("        Me.MaximizeBox = False")
a("        Me.Name = \"MiniForm\"")
a("        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen")
a("        Me.Text = \"HW Dollar4 Mini\"")
a("")

for s in reversed(shells):
    a("        Me.{0}.ResumeLayout(False)".format(s[0]))
a("        Me.PnlMsgShell.ResumeLayout(False)")
a("        Me.PnlTypeTag.ResumeLayout(False)")
a("        Me.PnlField.ResumeLayout(False)")
a("        Me.PnlCfg.ResumeLayout(False)")
a("        Me.ResumeLayout(False)")
a("")
a("    End Sub")
a("")
for n in decl:
    a("    Friend WithEvents {0} As {1}".format(n, types[n]))
a("End Class")

io.open(OUT, 'w', encoding='utf-8-sig', newline='\r\n').write('\n'.join(L))
print('written', OUT, len(L), 'lines')
