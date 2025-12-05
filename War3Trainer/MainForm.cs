using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace War3Trainer
{
    public partial class MainForm : Form
    {
        private GameContext _currentGameContext;
        private GameTrainer _mainTrainer;

        // 护甲锁定相关
        private bool _isArmorLocked = false;
        private System.Windows.Forms.Timer _armorLockTimer;
        private HashSet<UInt32> _lockedArmorAddresses = new HashSet<UInt32>(); // 存储所有已锁定的护甲地址
        private Dictionary<UInt32, WindowsApi.NativeMethods.MemoryProtection> _armorOriginalProtections = new Dictionary<UInt32, WindowsApi.NativeMethods.MemoryProtection>(); // 地址 -> 原始保护属性
        private float _lockedArmorValue = 2E+20f; // 护甲锁定值

        // 属性锁定相关（HP / MP / 护甲 / 攻击间隔 / 攻击范围）
        private bool _isAttributeLocked = false;
        private Dictionary<UInt32, float> _lockedAttributeValues = new Dictionary<UInt32, float>(); // 地址 -> 锁定值
        private Dictionary<UInt32, WindowsApi.NativeMethods.MemoryProtection> _attributeOriginalProtections = new Dictionary<UInt32, WindowsApi.NativeMethods.MemoryProtection>(); // 地址 -> 原始保护属性

        public MainForm()
        {
            InitializeComponent();
            SetRightGrid(RightFunction.Introduction);
            
            // 初始化护甲锁定定时器（可选，用于持续保护）
            _armorLockTimer = new System.Windows.Forms.Timer();
            _armorLockTimer.Interval = 100; // 每100毫秒检查一次
            _armorLockTimer.Tick += ArmorLockTimer_Tick;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.EnterDebugMode();
            }
            catch
            {
                ReportEnterDebugFailure();
                return;
            }

            FindGame();
        }

        /************************************************************************/
        /* Main functions                                                       */
        /************************************************************************/
        private void FindGame()
        {
            bool isRecognized = false;
            try
            {
                _currentGameContext = GameContext.FindGameRunning("war3", "game.dll");
                if (_currentGameContext == null)
                {
                    // netease war3 platform(dz.163.com)
                    _currentGameContext = GameContext.FindGameRunning("dzwar3", "game.dll");
                }
                if (_currentGameContext != null)
                {
                    // Game online
                    ReportVersionOk(_currentGameContext.ProcessId, _currentGameContext.ProcessVersion);

                    // Get a new trainer
                    GetAllObject();

                    isRecognized = true;
                }
                else
                {
                    // Game offline
                    ReportNoGameFoundFailure();
                }
            }
            catch (UnkonwnGameVersionExpection ex)
            {
                // Unknown game version
                _currentGameContext = null;
                ReportVersionFailure(ex.ProcessId, ex.GameVersion);
            }
            catch (WindowsApi.BadProcessIdException ex)
            {
                this._currentGameContext = null;
                ReportProcessIdFailure(ex.ProcessId);
            }
            catch (Exception ex)
            {
                // Why here?
                _currentGameContext = null;
                ReportUnknownFailure(ex.Message);
            }

            // Enable buttons
            if (isRecognized)
            {
                viewFunctions.Enabled = true;
                viewData.Enabled = true;
                cmdGetAllObjects.Enabled = true;
                cmdModify.Enabled = true;
            }
            else
            {
                viewFunctions.Enabled = false;
                viewData.Enabled = false;
                cmdGetAllObjects.Enabled = false;
                cmdModify.Enabled = false;
            }
        }

        private void GetAllObject()
        {
            // Check paramters
            if (_currentGameContext == null)
                return;

            // Get a new trainer
            _mainTrainer = new GameTrainer(_currentGameContext);

            // Create function tree
            viewFunctions.Nodes.Clear();
            foreach (ITrainerNode currentFunction in _mainTrainer.GetFunctionList())
            {
                TreeNode[] parentNodes = viewFunctions.Nodes.Find(currentFunction.ParentIndex.ToString(), true);
                TreeNodeCollection parentTree;
                if (parentNodes.Length < 1)
                    parentTree = viewFunctions.Nodes;
                else
                    parentTree = parentNodes[0].Nodes;

                parentTree.Add(
                    currentFunction.NodeIndex.ToString(),
                    currentFunction.NodeTypeName)
                    .Tag = currentFunction;
            }
            viewFunctions.ExpandAll();

            // Switch to page 1
            TreeNode[] introductionNodes = viewFunctions.Nodes.Find("1", true);
            if (introductionNodes.Length > 0)
            {
                viewFunctions.SelectedNode = introductionNodes[0];
                SelectFunction(introductionNodes[0]);
            }
        }

        // Re-query specific tree-node by FunctionListNode
        private void RefreshSelectedObject(ITrainerNode currentFunction)
        {
            TreeNode[] currentNodes = viewFunctions.Nodes.Find(currentFunction.NodeIndex.ToString(), true);
            TreeNode currentTree;
            if (currentNodes.Length < 1)
                return;
            else
                currentTree = currentNodes[0];

            currentTree.Text = currentFunction.NodeTypeName;
        }

        private void SelectFunction(TreeNode functionNode)
        {
            if (functionNode == null)
                return;
            ITrainerNode node = functionNode.Tag as ITrainerNode;
            if (node == null)
                return;

            // Show introduction page
            if (node.NodeType == TrainerNodeType.Introduction)
            {
                SetRightGrid(RightFunction.Introduction);
            }
            else
            {
                // Fill address list
                FillAddressList(node.NodeIndex);
                
                // Show address list
                if (viewData.Items.Count > 0)
                    SetRightGrid(RightFunction.EditTable);
                else
                    SetRightGrid(RightFunction.Empty);
            }            
        }

        private void FillAddressList(int functionNodeId)
        {
            // To set the right window
            viewData.Items.Clear();
            foreach (IAddressNode addressLine in _mainTrainer.GetAddressList())
            {
                if (addressLine.ParentIndex != functionNodeId)
                    continue;

                viewData.Items.Add(new ListViewItem(
                    new string[]
                    {
                        addressLine.Caption,    // Caption
                        "",                     // Original value
                        ""                      // Modified value
                    }));
                viewData.Items[viewData.Items.Count - 1].Tag = addressLine;
            }

            // To get memory content
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
            {
                foreach (ListViewItem currentItem in viewData.Items)
                {
                    IAddressNode addressLine = currentItem.Tag as IAddressNode;
                    if (addressLine == null)
                        continue;

                    // 如果护甲已锁定，恢复为锁定值
                    if (_isArmorLocked && addressLine.Caption == "盔甲 - 数量" && _lockedArmorAddresses.Contains(addressLine.Address))
                    {
                        mem.WriteFloat((IntPtr)addressLine.Address, _lockedArmorValue);
                        currentItem.SubItems[1].Text = _lockedArmorValue.ToString();
                        continue;
                    }

                    // 如果属性已锁定，恢复为锁定值
                    if (_isAttributeLocked && _lockedAttributeValues.TryGetValue(addressLine.Address, out float lockedValue))
                    {
                        mem.WriteFloat((IntPtr)addressLine.Address, lockedValue);
                        currentItem.SubItems[1].Text = lockedValue.ToString();
                        continue;
                    }

                    Object itemValue;
                    switch (addressLine.ValueType)
                    {
                        case AddressListValueType.Integer:
                            itemValue = mem.ReadInt32((IntPtr)addressLine.Address)
                                / addressLine.ValueScale;
                            break;
                        case AddressListValueType.Float:
                            itemValue = mem.ReadFloat((IntPtr)addressLine.Address)
                                / addressLine.ValueScale;
                            break;
                        case AddressListValueType.Char4:
                            itemValue = mem.ReadChar4((IntPtr)addressLine.Address);
                            break;
                        default:
                            itemValue = "";
                            break;
                    }
                    currentItem.SubItems[1].Text = itemValue.ToString();
                }
            }
        }

        // To apply the modifications
        private void ApplyModify()
        {
            using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
            {
                const float LOCKED_ARMOR_VALUE = 2E+20f;

                foreach (ListViewItem currentItem in viewData.Items)
                {
                    string itemValueString = currentItem.SubItems[2].Text;
                    if (String.IsNullOrEmpty(itemValueString))
                    {
                        // Not modified
                        continue;
                    }

                    IAddressNode addressLine = currentItem.Tag as IAddressNode;
                    if (addressLine == null)
                        continue;

                    // 如果护甲已锁定，阻止修改并恢复为锁定值
                    if (_isArmorLocked && addressLine.Caption == "盔甲 - 数量" && _lockedArmorAddresses.Contains(addressLine.Address))
                    {
                        // 跳过写入，直接恢复为锁定值
                        mem.WriteFloat((IntPtr)addressLine.Address, _lockedArmorValue);
                        currentItem.SubItems[2].Text = ""; // 清除修改标记
                        continue;
                    }

                    // 如果属性已锁定，阻止修改并恢复为锁定值
                    if (_isAttributeLocked && _lockedAttributeValues.TryGetValue(addressLine.Address, out float lockedValue))
                    {
                        mem.WriteFloat((IntPtr)addressLine.Address, lockedValue);
                        currentItem.SubItems[2].Text = "";
                        continue;
                    }

                    switch (addressLine.ValueType)
                    {
                        case AddressListValueType.Integer:
                            Int32 intValue;
                            if (!Int32.TryParse(itemValueString, out intValue))
                                intValue = 0;
                            intValue = unchecked(intValue * addressLine.ValueScale);
                            mem.WriteInt32((IntPtr)addressLine.Address, intValue);
                            break;
                        case AddressListValueType.Float:
                            float floatValue;
                            if (!float.TryParse(itemValueString, out floatValue))
                                floatValue = 0;
                            floatValue = unchecked(floatValue * addressLine.ValueScale);
                            mem.WriteFloat((IntPtr)addressLine.Address, floatValue);
                            break;
                        case AddressListValueType.Char4:
                            mem.WriteChar4((IntPtr)addressLine.Address, itemValueString);
                            break;
                    }
                    currentItem.SubItems[2].Text = "";
                }
            }
        }

        /************************************************************************/
        /* Exception UI                                                         */
        /************************************************************************/
        private void ReportEnterDebugFailure()
        {
            labGameScanState.Text = "请以管理员身份运行";
        }

        private void ReportNoGameFoundFailure()
        {
            labGameScanState.Text = "游戏未运行，运行游戏后单击“查找游戏”";
        }

        private void ReportUnknownFailure(string message)
        {
            labGameScanState.Text = "发生未知错误：" + message;
        }

        private void ReportProcessIdFailure(int processId)
        {
            labGameScanState.Text = "错误的进程ID："
                + processId.ToString();
        }

        private void ReportVersionFailure(int processId, string version)
        {
            labGameScanState.Text = "检测到游戏，但版本（"
                + version
                + "）不被支持";
        }

        private void ReportVersionOk(int processId, string version)
        {
            labGameScanState.Text = "检测到游戏（"
                + processId.ToString()
                + "），游戏版本："
                + version
                + "（支持）";
        }

        /************************************************************************/
        /* GUI                                                                  */
        /************************************************************************/
        private void MenuHelpAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("魔兽争霸3 冰封王座 内存修改器" + System.Environment.NewLine
                + Application.ProductVersion + System.Environment.NewLine
                + System.Environment.NewLine
                + "源代码在这里：https://github.com/tctianchi/War3Trainer" + System.Environment.NewLine
                + "学着自己动手改。" + System.Environment.NewLine
                + "",
                "War3Trainer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        
        private void MenuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cmdGetAllObjects_Click(object sender, EventArgs e)
        {
            try
            {
                GetAllObject();
            }
            catch (WindowsApi.BadProcessIdException ex)
            {
                ReportProcessIdFailure(ex.ProcessId);
            }
        }

        private void cmdScanGame_Click(object sender, EventArgs e)
        {
            FindGame();
        }

        private void cmdModify_Click(object sender, EventArgs e)
        {
            try
            {
                ApplyModify();

                // Refresh left
                TreeNode selectedNode = viewFunctions.SelectedNode;
                if (selectedNode == null)
                    return;

                ITrainerNode functionNode = selectedNode.Tag as ITrainerNode;
                if (functionNode != null)
                    RefreshSelectedObject(functionNode);

                // Refresh right
                SelectFunction(selectedNode);
            }
            catch (WindowsApi.BadProcessIdException ex)
            {
                ReportProcessIdFailure(ex.ProcessId);
            }
        }

        private void cmdLockAttributes_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentGameContext == null)
                {
                    MessageBox.Show("请先查找游戏！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 如果已经锁定，则解锁
                if (_isAttributeLocked)
                {
                    _armorLockTimer.Stop();
                    _isAttributeLocked = false;
                    _lockedAttributeValues.Clear();
                    _attributeOriginalProtections.Clear();
                    cmdLockAttributes.Text = "锁定属性";
                    cmdLockAttributes.BackColor = System.Drawing.SystemColors.Control;
                    MessageBox.Show("属性锁定已解除！", "解锁成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_mainTrainer == null)
                {
                    MessageBox.Show("尚未加载任何单位，请先点击“刷新”。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 从输入框读取锁定值
                float hpMpValue = 2E+12f;
                float attackIntervalValue = 0.2f;
                float attackRangeValue = 5000f;
                
                if (!float.TryParse(txtLockHpMpValue.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out hpMpValue))
                {
                    MessageBox.Show("HP/MP锁定值格式错误，请输入有效的数字（如：2E+12）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (!float.TryParse(txtLockAttackInterval.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attackIntervalValue))
                {
                    MessageBox.Show("攻击间隔锁定值格式错误，请输入有效的数字（如：0.2）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                if (!float.TryParse(txtLockAttackRange.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out attackRangeValue))
                {
                    MessageBox.Show("攻击范围锁定值格式错误，请输入有效的数字（如：5000）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // 可锁定的属性及其锁定值
                Dictionary<string, float> targetValues = new Dictionary<string, float>
                {
                    { "MP - 最大",  hpMpValue },
                    { "MP - 目前",  hpMpValue },
                    { "HP - 最大",  hpMpValue },
                    { "HP - 目前",  hpMpValue },
                    { "盔甲 - 数量", hpMpValue },
                    { "攻击1 - 间隔", attackIntervalValue },
                    { "攻击1 - 范围", attackRangeValue },
                };

                int lockedCount = 0;

                using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
                {
                    foreach (IAddressNode addressLine in _mainTrainer.GetAddressList())
                    {
                        if (!targetValues.TryGetValue(addressLine.Caption, out float value))
                            continue;

                        // 只处理浮点类型的属性
                        if (addressLine.ValueType != AddressListValueType.Float)
                            continue;

                        mem.WriteFloat((IntPtr)addressLine.Address, value);
                        _lockedAttributeValues[addressLine.Address] = value;
                        lockedCount++;
                    }
                }

                if (lockedCount == 0)
                {
                    MessageBox.Show("当前未找到可锁定的属性，请先选择一个单位或相关功能节点。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _isAttributeLocked = true;
                // 启动定时器，持续保护锁定的属性
                _armorLockTimer.Start();
                
                cmdLockAttributes.Text = "解锁属性";
                cmdLockAttributes.BackColor = System.Drawing.Color.LightSkyBlue;

                MessageBox.Show(
                    $"已锁定 {lockedCount} 个属性：\n\n" +
                    "保护方式：定时器持续写入（每100毫秒）\n\n" +
                    "可锁定的属性包括：\n" +
                    "- MP - 最大 / 目前\n" +
                    "- HP - 最大 / 目前\n" +
                    "- 盔甲 - 数量\n" +
                    "- 攻击1/2 - 间隔 / 范围\n" +
                    "- 移动速度\n" +
                    "- 视野范围\n" +
                    "- 转身速度\n" +
                    "- 攻击频率比\n\n" +
                    "当前锁定值：\n" +
                    $"- HP / MP / 盔甲 = {hpMpValue}\n" +
                    $"- 攻击间隔 = {attackIntervalValue}\n" +
                    $"- 攻击范围 = {attackRangeValue}",
                    "锁定成功",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (WindowsApi.BadProcessIdException ex)
            {
                ReportProcessIdFailure(ex.ProcessId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("锁定属性时发生错误：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmdLockArmor_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentGameContext == null)
                {
                    MessageBox.Show("请先查找游戏！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 如果已经锁定，则解锁
                if (_isArmorLocked)
                {
                    _armorLockTimer.Stop();
                    _isArmorLocked = false;
                    _lockedArmorAddresses.Clear();
                    _armorOriginalProtections.Clear();
                    cmdLockArmor.Text = "锁定护甲";
                    cmdLockArmor.BackColor = System.Drawing.SystemColors.Control;
                    MessageBox.Show("护甲锁定已解除！", "解锁成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 查找所有"盔甲 - 数量"的地址（可能有多个单位）
                List<UInt32> armorAddresses = new List<UInt32>();
                foreach (IAddressNode addressLine in _mainTrainer.GetAddressList())
                {
                    if (addressLine.Caption == "盔甲 - 数量")
                    {
                        armorAddresses.Add(addressLine.Address);
                    }
                }

                if (armorAddresses.Count == 0)
                {
                    MessageBox.Show("未找到护甲地址，请先选择一个单位！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 从输入框读取护甲锁定值
                if (!float.TryParse(txtLockArmorValue.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _lockedArmorValue))
                {
                    MessageBox.Show("护甲锁定值格式错误，请输入有效的数字（如：2E+20）", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // 设置所有找到的护甲值为用户输入的值
                using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
                {
                    foreach (UInt32 address in armorAddresses)
                    {
                        mem.WriteFloat((IntPtr)address, _lockedArmorValue);
                        _lockedArmorAddresses.Add(address); // 添加到锁定地址集合
                    }
                }

                // 启动锁定，启用定时器持续保护锁定的护甲
                _isArmorLocked = true;
                _armorLockTimer.Start(); // 启动定时器，防止游戏本身修改护甲值
                cmdLockArmor.Text = "解锁护甲";
                cmdLockArmor.BackColor = System.Drawing.Color.LightGreen;

                MessageBox.Show($"已锁定 {armorAddresses.Count} 个单位的护甲为 {_lockedArmorValue}！\n\n保护方式：\n- 定时器持续写入（每100毫秒）\n- 阻止通过修改器修改\n- 刷新时自动恢复", "锁定成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (WindowsApi.BadProcessIdException ex)
            {
                ReportProcessIdFailure(ex.ProcessId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("锁定护甲时发生错误：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ArmorLockTimer_Tick(object sender, EventArgs e)
        {
            if ((!_isArmorLocked && !_isAttributeLocked) || _currentGameContext == null)
            {
                _armorLockTimer.Stop();
                return;
            }

            try
            {
                using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
                {
                    // 持续锁定所有已记录的护甲地址（用于防止游戏本身修改）
                    if (_isArmorLocked)
                    {
                        foreach (UInt32 address in _lockedArmorAddresses)
                        {
                            mem.WriteFloat((IntPtr)address, _lockedArmorValue);
                        }
                    }

                    // 持续锁定所有已记录的属性地址（用于防止游戏本身修改）
                    if (_isAttributeLocked)
                    {
                        foreach (var kvp in _lockedAttributeValues)
                        {
                            mem.WriteFloat((IntPtr)kvp.Key, kvp.Value);
                        }
                    }
                }
            }
            catch
            {
                // 如果发生错误（如游戏关闭），停止锁定
                _armorLockTimer.Stop();
                _isArmorLocked = false;
                _isAttributeLocked = false;
                _lockedArmorAddresses.Clear();
                _lockedAttributeValues.Clear();
                cmdLockArmor.Text = "锁定护甲";
                cmdLockArmor.BackColor = System.Drawing.SystemColors.Control;
                cmdLockAttributes.Text = "锁定属性";
                cmdLockAttributes.BackColor = System.Drawing.SystemColors.Control;
            }
        }

        private void viewFunctions_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            // Check whether modification is not saved
            bool isSaved = true;
            foreach (ListViewItem currentItem in viewData.Items)
            {
                if (!String.IsNullOrEmpty(currentItem.SubItems[2].Text))
                {
                    isSaved = false;
                    break;
                }
            }

            // Save all if not saved
            if (!isSaved)
            {
                cmdModify_Click(this, null);
            }

            // Select another function
            try
            {
                SelectFunction(e.Node);
            }
            catch (WindowsApi.BadProcessIdException ex)
            {
                ReportProcessIdFailure(ex.ProcessId);
            }
        }

        private enum RightFunction
        {
            Empty,
            Introduction,
            EditTable,
        }

        private void SetRightGrid(RightFunction function)
        {
            this.splitMain.Panel2.SuspendLayout();
            this.viewData.SuspendLayout();

            txtIntroduction.Visible = function == RightFunction.Introduction;
            viewData.Visible = function == RightFunction.EditTable;
            lblEmpty.Visible = function == RightFunction.Empty;

            txtIntroduction.Dock = DockStyle.Fill;
            viewData.Dock = DockStyle.Fill;
            lblEmpty.Location = new Point(0, 0);

            this.viewData.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            this.splitMain.Panel2.PerformLayout();
        }

        //////////////////////////////////////////////////////////////////////////       
        // Make the ListView editable
        private void ReplaceInputTextbox()
        {
            if (viewData.SelectedItems.Count < 1)
                return;
            ListViewItem currentItem = viewData.SelectedItems[0];

            txtInput.Location = new Point(
                viewData.Columns[0].Width + viewData.Columns[1].Width,
                currentItem.Position.Y - 2);
            txtInput.Width = viewData.Columns[2].Width;
        }

        private void viewData_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch ((Keys)e.KeyChar)
            {
                case Keys.Enter:
                    viewData_MouseUp(sender, null);
                    e.Handled = true;
                    break;
            }
        }

        private void viewData_MouseUp(object sender, MouseEventArgs e)
        {
            // Get item
            if (viewData.SelectedItems.Count < 1)
                return;
            ListViewItem currentItem = viewData.SelectedItems[0];

            // Determine the content of edit box
            ReplaceInputTextbox();

            txtInput.Tag = currentItem;

            int textToEdit;
            if (String.IsNullOrEmpty(currentItem.SubItems[2].Text))
                textToEdit = 1;
            else
                textToEdit = 2;
            txtInput.Text = currentItem.SubItems[textToEdit].Text;

            // Enable editing
            txtInput.Visible = true;
            txtInput.Focus();
            txtInput.Select(0, 0);  // Cancel select all
        }

        private void viewData_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            ReplaceInputTextbox();
        }

        private void viewData_Scrolling(object sender, EventArgs e)
        {
            viewData.Focus();
        }

        private void txtInput_Leave(object sender, EventArgs e)
        {
            txtInput.Visible = false;
            ListViewItem currentItem = txtInput.Tag as ListViewItem;
            if (currentItem == null)
                return;

            if (currentItem.SubItems[1].Text != txtInput.Text)
                currentItem.SubItems[2].Text = txtInput.Text;
            else
                currentItem.SubItems[2].Text = "";
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    CommitEditAndMoveNext(sender, 1);
                    e.Handled = true;
                    break;
                case Keys.Up:
                    CommitEditAndMoveNext(sender, -1);
                    e.Handled = true;
                    break;
                case Keys.Down:
                    CommitEditAndMoveNext(sender, 1);
                    e.Handled = true;
                    break;
                case Keys.Escape:
                    DiscardEdit(sender);
                    e.Handled = true;
                    break;
            }
        }

        private void DiscardEdit(object editBox)
        {
            // Roll back content of the edit box
            viewData_MouseUp(editBox, null);

            // Hide edit box
            txtInput_Leave(editBox, null);

            // Restore focus
            viewData.Focus();
        }

        private void CommitEditAndMoveNext(object editBox, int delta)
        {
            // Commit
            txtInput_Leave(editBox, null);

            // Move to another line
            viewData.Focus();
            if (viewData.SelectedItems.Count > 0)
            {
                int nextIndex = viewData.SelectedItems[0].Index + delta;
                if (nextIndex < viewData.Items.Count &&
                    nextIndex >= 0)
                {
                    viewData.Items[nextIndex].Selected = true;
                    viewData.Items[nextIndex].Focused = true;
                    viewData.Items[nextIndex].EnsureVisible();
                }
                viewData_MouseUp(editBox, null);
            }
        }

        /************************************************************************/
        /* Debug                                                                */
        /************************************************************************/
        private void menuDebug1_Click(object sender, EventArgs e)
        {
            string strIndex = Microsoft.VisualBasic.Interaction.InputBox(
                "nIndex = 0x?",
                "War3Common.ReadFromGameMemory(nIndex)",
                "0", -1, -1);
            if (String.IsNullOrEmpty(strIndex))
                return;

            Int32 nIndex;
            if (!Int32.TryParse(
                strIndex,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.NumberFormatInfo.InvariantInfo,
                out nIndex))
            {
                nIndex = 0;
            }

            try
            {
                UInt32 result = 0;
                using (WindowsApi.ProcessMemory mem = new WindowsApi.ProcessMemory(_currentGameContext.ProcessId))
                {
                    NewChildrenEventArgs args = new NewChildrenEventArgs();
                    War3Common.GetGameMemory(
                        _currentGameContext, ref args);
                    result = War3Common.ReadFromGameMemory(
                        mem, _currentGameContext, args,
                        nIndex);
                }
                MessageBox.Show(
                    "0x" + result.ToString("X"),
                    "War3Common.ReadFromGameMemory(0x" + strIndex + ")");
            }
            catch (WindowsApi.BadProcessIdException ex)
            {
                ReportProcessIdFailure(ex.ProcessId);
            }
        }
    }
}

