using System;
using System.Data;
using System.Windows.Forms;
using FANNCSharp;
using System.Data.OleDb;
using System.IO;
using Accord.Math;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
//using System.Threading.Tasks;

namespace FANN_WindowsFormsApplication
{
    public partial class Form1 : Form
    {
        private FANNClass FANN;
        private string[] AddDataMixToDataGridView; //Для асинхронного перемешивания тренировочных данных в DataMix
        private int ThreadSleepAddDataMix = 50;
        int DataMixGoodCount = 0;
        int DataMixBadCount = 0;
        int DataMixLoopRowCount = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private string GetDirectoryName()
        {
            string location = System.Reflection.Assembly.GetEntryAssembly().Location;
            return System.IO.Path.GetDirectoryName(location);
        }

        private DataTable LoadExcelData(string pathName)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(pathName);
            DataTable tbContainer = new DataTable();
            string strConn = string.Empty;
            string sheetName = fileName;

            FileInfo file = new FileInfo(pathName);
            if (!file.Exists) { throw new Exception("Error, file doesn't exists!"); }
            string extension = file.Extension;
            switch (extension)
            {
                case ".xls":
                    strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathName + ";Extended Properties='Excel 8.0;HDR=Yes;IMEX=1;'";
                    break;
                case ".xlsx":
                    strConn = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + pathName + ";Extended Properties='Excel 12.0;HDR=Yes;IMEX=1;'";
                    break;
                default:
                    strConn = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathName + ";Extended Properties='Excel 8.0;HDR=Yes;IMEX=1;'";
                    break;
            }
            OleDbConnection cnnxls = new OleDbConnection(strConn);
            OleDbDataAdapter oda = new OleDbDataAdapter(string.Format("select * from [{0}$]", sheetName), cnnxls);
            oda.Fill(tbContainer);

            return tbContainer;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (var item in Enum.GetValues(typeof(FANNCSharp.ActivationFunction))) { comboBox1.Items.Add(item); } //AFH
            foreach (var item in Enum.GetValues(typeof(FANNCSharp.ActivationFunction))) { comboBox2.Items.Add(item); } //AFO
            foreach (var item in Enum.GetValues(typeof(FANNCSharp.StopFunction))) { comboBox3.Items.Add(item); } //TSF
            foreach (var item in Enum.GetValues(typeof(FANNCSharp.TrainingAlgorithm))) { comboBox4.Items.Add(item); } //TA

            SetTrainDefaultParameters();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            uint num_layers = Convert.ToUInt32(textBox5.Text);
            uint num_input = Convert.ToUInt32(textBox6.Text);
            uint num_neurons_hidden = Convert.ToUInt32(textBox7.Text);
            uint num_output = Convert.ToUInt32(textBox8.Text);

            float desired_error = 0;
            //uint epochs_between_reports = 10;
            FANN = new FANNClass(true, num_layers, num_input, num_neurons_hidden, num_output);

            toolStripStatusLabelInputNum.Text = num_input.ToString();
            toolStripStatusLabelLayersNum.Text = num_layers.ToString();
            toolStripStatusLabelNeuronsHiddenNum.Text = num_neurons_hidden.ToString();
            toolStripStatusLabelOutputNum.Text = num_output.ToString();

            //textBoxLog.AppendText(FANN.LogResult + "\n");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = GetDirectoryName();
            openFileDialog1.Title = "Browse FANN .net Files";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            openFileDialog1.DefaultExt = "net";
            openFileDialog1.Filter = "Net files (*.net)|*.net|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            //openFileDialog1.RestoreDirectory = true;

            //openFileDialog1.ReadOnlyChecked = true;
            //openFileDialog1.ShowReadOnly = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = openFileDialog1.FileName;
                FANN = new FANNClass();
                FANN.LoadNet(openFileDialog1.FileName);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            if (textBox4.Text == "")
            {
                button9_Click(sender, e);
            }
            string DataPath = textBox4.Text;
            uint epoch = Convert.ToUInt32( textBox1.Text );
            bool DefaultTrainParams = checkBox1.Checked;
            bool ScaleInput = checkBox2.Checked;
            if (ScaleInput) button11_Click(sender, e);
            button10_Click(sender, e);
            toolStripStatusLabelMSENum.Text = FANN.TrainOnData(DataPath, epoch, ScaleInput, DefaultTrainParams).ToString();

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult+"\r\n");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (textBox4.Text == "")
            {
                button9_Click(sender, e);
            }
            string DataPath = textBox4.Text;
            int trainCount = Convert.ToInt32(textBox10.Text);
            bool DefaultTrainParams = checkBox1.Checked;
            bool ScaleInput = checkBox2.Checked;
            button10_Click(sender, e);
            toolStripStatusLabelMSENum.Text = FANN.TrainOnDataEpoch(DataPath, trainCount, ScaleInput, DefaultTrainParams).ToString();

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private double[][] DataGridViewToArrays(DataGridView DGV)
        {
            double[,] inputs2d = new double[DGV.Rows.Count-1, DGV.Columns.Count];//-1 потому что таблица автоматически расширяется при заполнении
            for (int x = 0; x < inputs2d.GetLength(0); x++)
                for (int i = 0; i < inputs2d.GetLength(1); i++)
                    inputs2d[x, i] = Convert.ToDouble(DGV.Rows[x].Cells[i].Value);
            return inputs2d.ToJagged();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            double[][] inputs;
            double[][] outputs;

            inputs = DataGridViewToArrays(dataGridView1);
            outputs = DataGridViewToArrays(dataGridView2);

            int epoch = Convert.ToInt32(textBox17.Text);
            bool DefaultTrainParams = checkBox1.Checked;
            bool ScaleInput = checkBox2.Checked;
            if (ScaleInput) button11_Click(sender, e);
            button10_Click(sender, e);

            /*for (int i = 0; i < inputs.Length; i++) {
                FANN.TrainOnIO(inputs[i], outputs[i], epoch, DefaultTrainParams);
            }*/
            
            toolStripStatusLabelMSENum.Text = FANN.TrainOnIO(inputs, outputs, epoch, ScaleInput, DefaultTrainParams).ToString();

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = GetDirectoryName();
            saveFileDialog1.Title = "Save FANN File";

            saveFileDialog1.CheckPathExists = true;

            saveFileDialog1.DefaultExt = "net";
            saveFileDialog1.Filter = "Net files (*.net)|*.net|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            //saveFileDialog1.RestoreDirectory = true;

            //saveFileDialog1.ReadOnlyChecked = true;
            //saveFileDialog1.ShowReadOnly = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox3.Text = saveFileDialog1.FileName;
                FANN.SaveNet(saveFileDialog1.FileName, false);
            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = GetDirectoryName();
            openFileDialog1.Title = "Browse FANN Data files";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            openFileDialog1.DefaultExt = "data";
            openFileDialog1.Filter = "Data files (*.data)|*.data|Train files (*.train)|*.train|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            //openFileDialog1.RestoreDirectory = true;

            //openFileDialog1.ReadOnlyChecked = true;
            //openFileDialog1.ShowReadOnly = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = openFileDialog1.FileName;
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            ActivationFunction ActivationFunctionHidden;
            ActivationFunction ActivationFunctionOutput;
            StopFunction TrainStopFunction;
            double BitFailLimit;
            TrainingAlgorithm TrainingAlgorithm;

            if (checkBox1.Checked)
            {
                SetTrainDefaultParameters();
            }
            ActivationFunctionHidden = (ActivationFunction)comboBox1.SelectedIndex;
            ActivationFunctionOutput = (ActivationFunction)comboBox2.SelectedIndex;
            TrainStopFunction = (StopFunction)comboBox3.SelectedIndex;
            BitFailLimit = Convert.ToDouble(textBox9.Text.Replace('.', ','));
            TrainingAlgorithm = (TrainingAlgorithm)comboBox4.SelectedIndex;
            FANN.SetNetTrainParams(ActivationFunctionHidden, ActivationFunctionOutput, TrainStopFunction, BitFailLimit, TrainingAlgorithm);
        }

        private void SetTrainDefaultParameters()
        {
            comboBox1.SelectedIndex = 5;
            comboBox2.SelectedIndex = 5;
            comboBox3.SelectedIndex = 1;
            comboBox4.SelectedIndex = 2;
            textBox9.Text = "0.01";
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                SetTrainDefaultParameters();
            }
            
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            float ScaleNewInputMin = Convert.ToSingle(textBox11.Text.Replace('.', ','));
            float ScaleNewInputMax = Convert.ToSingle(textBox12.Text.Replace('.', ','));
            float ScaleNewOutputMin = Convert.ToSingle(textBox13.Text.Replace('.', ','));
            float ScaleNewOutputMax = Convert.ToSingle(textBox14.Text.Replace('.', ','));
            if (FANN == null) button1_Click(sender, e);
            FANN.SetScalingParamsValues(ScaleNewInputMin, ScaleNewInputMax, ScaleNewOutputMin, ScaleNewOutputMax);
        }

        private void DataGridViewCreateColumns(string Text, DataGridView DGV)
        {
            string[] separators = { ",", ".", ";", "#" };
            string value = Text;
            string[] parts = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 654) return;
            DGV.ColumnCount = parts.Length;
            for (int i = 0; i < parts.Length; i++)
            {
                DGV.Columns[i].Name = parts[i];
            }
        }
        private void button12_Click(object sender, EventArgs e)
        {
            DataGridViewCreateColumns(textBox16.Text, dataGridView1);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            DataGridViewCreateColumns(textBox15.Text, dataGridView2);
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            double[] inputs = new double[] { 0, -100 };
            double[] outputs = new double[] { -15 };
            string DataPath = textBox4.Text;
            bool ScaleInput = checkBox2.Checked;
            bool DefaultTrainParams = checkBox1.Checked;
            if (ScaleInput) button11_Click(sender, e); //Scale
            button10_Click(sender, e); //Train parameters
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            toolStripStatusLabelMSENum.Text = FANN.TrainOnIO(inputs, outputs, 100, DefaultTrainParams).ToString();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            double[,] array2d = Matrix.Parse("1\t 2\n3\t 4");
            var rows = array2d.GetLength(0);
            var cols = array2d.GetLength(1);
            var array1d = new double[rows * cols];
            var current = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    array1d[current++] = array2d[i, j];
                }
            }
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (FANN == null) FANN.Dispose(); //Delete Neuronet
        }

        private double[] DataGridViewTo1dArrays(DataGridView DGV)
        {
            double[] inputs2d = new double[DGV.Columns.Count];//-1 потому что таблица автоматически расширяется при заполнении
            for (int i = 0; i < inputs2d.Length; i++)
                inputs2d[i] = Convert.ToDouble(DGV.Rows[0].Cells[i].Value);
            return inputs2d;
        }
    
        private string DataGridViewSetRunResult(double[] Result, DataGridView DGV)
        {
            DGV.Rows.Clear();
            DGV.ColumnCount = Result.Length;
            string[] row = new string[Result.Length];
            for (int i = 0; i < Result.Length; i++)
            {
                DGV.Columns[i].Name = "Col " + i.ToString();
                row[i] = Result[i].ToString();
            }
            DGV.Rows.Add(row); //Добавление элементов в колонны
            return String.Join(" ", row);
        }

        private void button16_Click_1(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            double[] inputs;
            double[] outputs;
            double[] result;

            inputs = DataGridViewTo1dArrays(dataGridView3);
            outputs = DataGridViewTo1dArrays(dataGridView4);

            bool ScaleInput = checkBox3.Checked;
            //if (ScaleInput) button11_Click(sender, e); //Scale
            //button10_Click(sender, e); //Train parameters
            result = FANN.RunNetOnData(inputs, outputs, ScaleInput);
            //result = FANN.RunNetOnData(inputs, ScaleInput);
            toolStripStatusLabelMSENum.Text = DataGridViewSetRunResult(result, dataGridView5);

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private void button15_Click_1(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            if (textBox4.Text == "")
            {
                button9_Click(sender, e);
            }
            string DataPath = textBox4.Text;
            bool ScaleInput = checkBox3.Checked;
            if (ScaleInput) button11_Click(sender, e); //Scale
            button10_Click(sender, e); //Train parameters
            toolStripStatusLabelMSENum.Text = FANN.RunNetOnData(DataPath, ScaleInput).ToString();

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private void button7_Click_2(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            double[] inputs;
            double[] outputs;

            inputs = DataGridViewTo1dArrays(dataGridView3);
            outputs = DataGridViewTo1dArrays(dataGridView4);

            toolStripStatusLabelMSENum.Text = FANN.TestNetOnIO(inputs, outputs).ToString();
            //FANN.TrainOnIO(inputs[0], outputs[0], 100, true);
        }

        private void button6_Click_2(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            if (textBox4.Text == "")
            {
                button9_Click(sender, e);
            }
            string DataPath = textBox4.Text;
            bool ScaleInput = checkBox2.Checked;
            if (ScaleInput) button11_Click(sender, e); //Scale
            
            toolStripStatusLabelMSENum.Text = FANN.TestNetOnData(DataPath, ScaleInput).ToString();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            DataGridViewCreateColumns(textBox18.Text, dataGridView3);
        }

        private void button19_Click(object sender, EventArgs e)
        {
            DataGridViewCreateColumns(textBox19.Text, dataGridView4);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            uint num_layers = Convert.ToUInt32(textBox5.Text);
            uint num_input = Convert.ToUInt32(textBox6.Text);
            uint num_neurons_hidden = Convert.ToUInt32(textBox7.Text);
            uint num_output = Convert.ToUInt32(textBox8.Text);

            string[] separators = { ",", ".", ";", "#" };
            string value = textBox20.Text;
            string[] parts = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            List<uint> layers = new List<uint>();
            for (int i = 0; i < parts.Length; i++)
            {
                layers.Add(Convert.ToUInt32(parts[i]));
            }
            FANN = new FANNClass(true, layers);

            toolStripStatusLabelInputNum.Text = parts[0].ToString();
            toolStripStatusLabelLayersNum.Text = (parts.Length - 2).ToString();
            toolStripStatusLabelOutputNum.Text = parts[parts.Length-1].ToString();
            parts = parts.RemoveAt(0);
            parts = parts.RemoveAt(parts.Length - 1);
            toolStripStatusLabelNeuronsHiddenNum.Text = String.Join("; ", parts);

            //textBoxLog.AppendText(FANN.LogResult + "\n");
        }

        private void button21_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            double[] inputs;
            double[] result;

            inputs = DataGridViewTo1dArrays(dataGridView3);

            bool ScaleInput = checkBox3.Checked;
            //if (ScaleInput) button11_Click(sender, e); //Scale
            //button10_Click(sender, e); //Train parameters
            result = FANN.RunNetOnData(inputs, ScaleInput);
            toolStripStatusLabelMSENum.Text = DataGridViewSetRunResult(result, dataGridView5);

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private void button22_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            FANN.AddTrainIOToList(textBox21.Text, textBox22.Text);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            string DataPath = textBox4.Text;
            int epoch = Convert.ToInt32(textBox1.Text);
            bool DefaultTrainParams = checkBox1.Checked;
            bool ScaleInput = checkBox4.Checked;
            if (ScaleInput) button11_Click(sender, e);
            button10_Click(sender, e);
            FANN.TrainOnIOList(epoch, ScaleInput, DefaultTrainParams);

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private void button24_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = GetDirectoryName();
            saveFileDialog1.Title = "Save FANN Train File";

            saveFileDialog1.CheckPathExists = true;

            saveFileDialog1.DefaultExt = "data";
            saveFileDialog1.Filter = "Data files (*.data)|*.data|Train files (*.train)|*.train|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            //saveFileDialog1.RestoreDirectory = true;

            //saveFileDialog1.ReadOnlyChecked = true;
            //saveFileDialog1.ShowReadOnly = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (FANN == null) button1_Click(sender, e); //Create Neuronet
                bool ScaleInput = checkBox4.Checked;
                if (ScaleInput) button11_Click(sender, e);
                button10_Click(sender, e);
                FANN.SaveTrainIOList(saveFileDialog1.FileName, ScaleInput, true);
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = GetDirectoryName();
            saveFileDialog1.Title = "Save FANN Train File";

            saveFileDialog1.CheckPathExists = true;

            saveFileDialog1.DefaultExt = "net";
            saveFileDialog1.Filter = "Data files (*.data)|*.data|Train files (*.train)|*.train|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (FANN == null) button1_Click(sender, e); //Create Neuronet
                textBox23.Text = saveFileDialog1.FileName;
                bool ScaleInput = checkBox2.Checked;
                if (ScaleInput) button11_Click(sender, e);
                button10_Click(sender, e);
                FANN.SaveTrain(saveFileDialog1.FileName, true);
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            int Iterations = Convert.ToInt32(textBox24.Text);
            double error = Convert.ToDouble(textBox25.Text);
            double RndMin = Convert.ToDouble(textBox26.Text);
            double RndMax = Convert.ToDouble(textBox27.Text);
            bool ScaleInput = checkBox5.Checked;
            if (ScaleInput) button11_Click(sender, e);
            button10_Click(sender, e);
            double[] outputs = DataGridViewTo1dArrays(dataGridView4);

            //double[] R = { 0 };
            double[] result = FANN.GetNetResultInput(outputs, error, Iterations, RndMin, RndMax, ScaleInput);
            DataGridViewSetRunResult(result, dataGridView5);

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private void textBox20_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void button27_Click(object sender, EventArgs e)
        {
            textBoxLog.Clear();
        }

        private void button28_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = GetDirectoryName();
            saveFileDialog1.Title = "Save FANN Train Columns File";

            saveFileDialog1.CheckPathExists = true;

            saveFileDialog1.DefaultExt = "data";
            saveFileDialog1.Filter = "CData files(*.cdata) | *.cdata | CTrain files(*.ctrain) | *.ctrain | Txt files(*.txt) | *.txt | All files(*.*) | *.* ";
            saveFileDialog1.FilterIndex = 2;
            //saveFileDialog1.RestoreDirectory = true;

            //saveFileDialog1.ReadOnlyChecked = true;
            //saveFileDialog1.ShowReadOnly = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (FANN == null) button1_Click(sender, e); //Create Neuronet
                bool ScaleInput = checkBox4.Checked;
                if (ScaleInput) button11_Click(sender, e);
                button10_Click(sender, e);
                FANN.SaveTrainIOListToColumns(saveFileDialog1.FileName);

                DateTime DT = DateTimeOffset.Now.LocalDateTime;
                textBoxLog.AppendText(DT.ToString() + "\r\n" + FANN.LogResult + "\r\n");
            }
        }

        private void button29_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            int Iterations = Convert.ToInt32(textBox24.Text);
            double error = Convert.ToDouble(textBox25.Text);
            double RndMin = Convert.ToDouble(textBox26.Text);
            double RndMax = Convert.ToDouble(textBox27.Text);
            double[] outputs = DataGridViewTo1dArrays(dataGridView4);

            //double[] R = { 0 };
            double[] result = FANN.GetNetResultInput(Predict.__Spreadsh_MLP_3_9_2, 3, 1, outputs, error, Iterations, RndMin, RndMax);
            DataGridViewSetRunResult(result, dataGridView5);
            
            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private string[][] ParseDataToStr(string StrText)
        {
            string[] TextRows, TextCol;
            string[][] TextData;
            string input = StrText;
            string pattern = "\r\n";
            string pattern_tab = "\t";
            TextRows = Regex.Split(input, pattern); //Строки
            TextData = new string[TextRows.Length][];
            for (int i = 0; i< TextRows.Length; i++)
            {
                TextCol = Regex.Split(TextRows[i], pattern_tab); //Колонны
                TextData[i] = new string[TextCol.Length];
                for (int j = 0; j < TextCol.Length; j++)
                {
                    TextData[i][j] = TextCol[j];
                }
            }
            return TextData;
        }

        private double[][] ParseDataToDouble(string StrText)
        {
            string[] TextRows, TextCol;
            double[][] TextData;
            string input = StrText;
            string pattern = "\r\n";
            string pattern_tab = "\t";
            TextRows = Regex.Split(input, pattern); //Строки
            TextData = new double[TextRows.Length][];
            for (int i = 0; i < TextRows.Length; i++)
            {
                TextCol = Regex.Split(TextRows[i], pattern_tab); //Колонны
                TextData[i] = new double[TextCol.Length];
                for (int j = 0; j < TextCol.Length; j++)
                {
                    TextData[i][j] = Convert.ToDouble( TextCol[j] );
                }
            }
            return TextData;
        }

        private void DataGridViewSetData(string[][] Data, DataGridView DGV)
        {
            
            //DGV.RowCount = Data.Length-1;
            //DGV.ColumnCount = Data[0].Length;
            string[] row = new string[Data[0].Length];
            for (int i = 1; i < Data.Length - 1; i++) //Начало не с 0, т.к. 0 это заголовки, но до конца Data.Length! 
            {
                for (int j = 0; j < Data[i].Length; j++) //Начало не с 0, т.к. 0 это заголовки
                {
                    row[j] = Data[i][j];
                    //DGV[i, j].Value = Data[i][j]; //это если заранее было DGV.RowCount = Data.Length-1;
                }
                DGV.Rows.Add(row); //Добавление элементов в колонны
            }
        }

        private void button30_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = GetDirectoryName();
            openFileDialog1.Title = "Browse DataMix .cdata Files";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            openFileDialog1.DefaultExt = "data";
            openFileDialog1.Filter = "CData files (*.cdata)|*.cdata|CTrain files (*.ctrain)|*.ctrain|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            //openFileDialog1.RestoreDirectory = true;

            //openFileDialog1.ReadOnlyChecked = true;
            //openFileDialog1.ShowReadOnly = true;
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox28.Text = openFileDialog1.FileName;
                button31_Click(sender, e); //Парсинг файла
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            string TextData = "";
            string DataHeaders = "";
            string DataHeadersTranspose = "";
            string[][] ParsedData;
            if (checkBox6.Checked)
            {
                TextData = textBox32.Text; //Загружаем текст
            }
            else
            {
                try
                {   // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(textBox28.Text))
                    {
                        TextData = sr.ReadToEnd();
                    }
                }
                catch { };
            }
            if (TextData.Trim() == "") return;
            TextData = TextData.Replace(".", ",");
            ParsedData = ParseDataToStr(TextData);
            dataGridView6.Rows.Clear();
            dataGridView6.Columns.Clear();
            dataGridView7.Rows.Clear();
            dataGridView7.Columns.Clear();
            if (checkBox7.Checked) //Загружаем данные транспонировано
            { 
                DataHeadersTranspose = ParsedData[0][0] + ";";
                for (int i = 1; i < ParsedData.Length - 1; i++) //Заголовки таблицы
                {
                    DataHeadersTranspose += "out " + ParsedData[i][1] + ";";
                }
                //DataGridViewCreateColumns(DataHeaders, dataGridView6);
                DataGridViewCreateColumns(DataHeadersTranspose, dataGridView7);
            }
            for (int i = 0; i < ParsedData[0].Length; i++) //Заголовки таблицы
            {
                DataHeaders += ParsedData[0][i] + ";";
            }
            DataGridViewCreateColumns(DataHeaders, dataGridView6);
            //DataGridViewCreateColumns(DataHeaders + ";Result", dataGridView7);
            DataGridViewSetData(ParsedData, dataGridView6);
            
            textBox32.Text = TextData;
        }

        private void button32_Click(object sender, EventArgs e)
        {
            if (textBox33.Text == "" || textBox28.Text == "")
            {
                button31_Click(sender, e);
            }
            if (dataGridView6.RowCount < 1) return;
            dataGridView7.Rows.Clear();
            textBox33.Clear();
            dataGridView7.Visible = false;
            textBox33.Visible = false;
            int RepeatCount = Convert.ToInt32(textBox31.Text);
            toolStripProgressBar1.Minimum = 0;
            toolStripProgressBar1.Maximum = RepeatCount;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = dataGridView6.RowCount;

            button32.Enabled = false;
            ThreadSleepAddDataMix = Convert.ToInt32(textBox34.Text);
            string columnsName = "";
            if (dataGridView7.ColumnCount > 0)
            {
                for (int i = 0; i < dataGridView7.ColumnCount; i++)
                {
                    columnsName += dataGridView7.Columns[i].Name + "\t";
                }
                columnsName = columnsName.Remove(columnsName.Length - 1);
            }
            
            textBox33.Text = columnsName + "\r\n";
            
            backgroundWorker1.RunWorkerAsync();
            //dataGridView7.Visible = true;

        }

        private void button33_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = GetDirectoryName();
            saveFileDialog1.Title = "Save DataMix Train File";

            saveFileDialog1.CheckPathExists = true;

            saveFileDialog1.DefaultExt = "data";
            saveFileDialog1.Filter = "CData files (*.cdata)|*.cdata|CTrain files (*.ctrain)|*.ctrain|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            //saveFileDialog1.RestoreDirectory = true;

            //saveFileDialog1.ReadOnlyChecked = true;
            //saveFileDialog1.ShowReadOnly = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog1.FileName, textBox33.Text);
            }
        }

        private void DataMixTrainTranspose(Random rnd, int i)
        {
            int RepeatCount = Convert.ToInt32(textBox31.Text);
            string TrainCheck = textBox32.Text;
            string ResultGoodText = textBox30.Text;
            string ResultBadText = textBox29.Text;
            float percentGood = 0f;
            float percentGoodParam = Convert.ToInt32(textBox35.Text) / 100f;
            string TrainResultToCheck;
            string TrainResultsTranspose;
            string Result, totalResult;
            string[] input = new string[dataGridView6.ColumnCount];
            totalResult = "";
            TrainResultToCheck = "";
            TrainResultsTranspose = "";
            Result = "";
            int Row = 0;
            TrainResultToCheck = "";
            input[0] = dataGridView6[0, i].Value.ToString();
            for (int p = 0; p < dataGridView6.RowCount - 1; p++)
            {
                TrainResultToCheck = "";
                input[1] = dataGridView6[1, p].Value.ToString();
                for (int k = 0; k < input.Length; k++)
                {
                    TrainResultToCheck += input[k] + "\t";
                }
                TrainResultToCheck = TrainResultToCheck.Remove(TrainResultToCheck.Length - 1);
                TrainResultToCheck = TrainResultToCheck.Insert(0, "\r\n");
                TrainResultToCheck = TrainResultToCheck.Insert(TrainResultToCheck.Length, "\r\n");
                if (TrainCheck.IndexOf(TrainResultToCheck) > 0) //Если есть такая комбинация
                {
                    Result = ResultGoodText;
                    //string tabs = new String('\t', Row);
                    //AddDataMixToDataGridView += TrainResultToCheck;
                    DataMixGoodCount++;
                }
                else
                {
                    Result = ResultBadText;
                    DataMixBadCount++;
                }
                totalResult += Result + "\t";
                    
            }
            totalResult = totalResult.Remove(totalResult.Length - 1);
            TrainResultsTranspose += input[0] + "\t" + totalResult;
            //totalResult += TrainResultsTranspose + "\r\n";
            AddDataMixToDataGridView = TrainResultsTranspose.Split(new char[] { '\t' });
        }

        private void DataMixTrainNormal(Random rnd, int i)
        {
            int RepeatCount = Convert.ToInt32(textBox31.Text);
            string TrainCheck = textBox32.Text;
            string ResultGoodText = textBox30.Text;
            string ResultBadText = textBox29.Text;
            float percentGood = 0f;
            float percentGoodParam = Convert.ToInt32(textBox35.Text) / 100f;
            string TrainResultToCheck;
            string Result, totalResult;
            string[] input = new string[dataGridView6.ColumnCount];
            totalResult = "";
            TrainResultToCheck = "";
            input[0] = dataGridView6[0, i].Value.ToString();
            for (int j = 1; j < input.Length; j++)
            {
                if (DataMixGoodCount + DataMixBadCount != 0)
                {
                    percentGood = (float)DataMixGoodCount / (float)(DataMixGoodCount + DataMixBadCount);
                }
                if (percentGoodParam >= percentGood)
                {
                    input[j] = dataGridView6[j, i].Value.ToString();
                }
                else
                {
                    int rndRow = rnd.Next(dataGridView6.RowCount - 1);
                    //Debug.WriteLine("rndRow=" + rndRow);
                    input[j] = dataGridView6[j, rndRow].Value.ToString();
                }
            }
            for (int k = 0; k < input.Length; k++)
            {
                TrainResultToCheck += input[k] + "\t";
            }
            TrainResultToCheck = TrainResultToCheck.Remove(TrainResultToCheck.Length - 1);
            if (TrainCheck.IndexOf(TrainResultToCheck) > 0) //Если есть такая комбинация
            {
                Result = ResultGoodText;
                DataMixGoodCount++;
            }
            else
            {
                Result = ResultBadText;
                DataMixBadCount++;
            }

            TrainResultToCheck += "\t" + Result;
            totalResult += TrainResultToCheck + "\r\n";
            AddDataMixToDataGridView = TrainResultToCheck.Split(new char[] { '\t' });
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            int RepeatCount = Convert.ToInt32(textBox31.Text);
            Random rnd = new Random();
            DataMixGoodCount = 0;
            DataMixBadCount = 0;
            //MessageBox.Show(dataGridView6[1,1].Value.ToString());
            for (int n = 1; n <= RepeatCount; n++)
            {
                
                for (int i = 0; i < dataGridView6.RowCount - 1; i++)
                {
                    if (backgroundWorker1.CancellationPending)
                    {
                        e.Cancel = true;
                        backgroundWorker1.ReportProgress(0);
                        return;
                    }
                    if (checkBox7.Checked) //Загружаем данные транспонировано
                    {
                        DataMixTrainTranspose(rnd, i);
                    }
                    else
                    {
                        DataMixTrainNormal(rnd, i);
                    }
                    //dataGridView7.Rows.Add(TrainResultToCheck.Split(new char[] { '\t' }));
                    DataMixLoopRowCount = i;
                    backgroundWorker1.ReportProgress(n);
                    Thread.Sleep(ThreadSleepAddDataMix);
                    
                    //Invoke(new Action(UpdateDataGridView)); // Это вызов из Thread или Task.
                    //dataGridViewAddRow(dataGridView7, TrainResultToCheck.Split(new char[] { '\t' }));
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (dataGridView7.ColumnCount > 0)
            {
                dataGridView7.Rows.Add(AddDataMixToDataGridView); //Любое общение с формой. Переменные брать только глобальные
            }
            textBox33.Text += string.Join("\t", AddDataMixToDataGridView) + "\r\n";
            toolStripProgressBar1.Value = e.ProgressPercentage;
            progressBar1.Value = DataMixLoopRowCount;
        }

        private void button34_Click(object sender, EventArgs e)
        {
            //Check if background worker is doing anything and send a cancellation if it is
            if (backgroundWorker1.IsBusy)
            {
                backgroundWorker1.CancelAsync();
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                button32.Enabled = true;
            }
            else if (e.Error != null)
            {
                button32.Enabled = true;
            }
            else
            {
                button32.Enabled = true;
            }
            progressBar1.Value = progressBar1.Maximum;
            dataGridView7.Visible = true;
            textBox33.Visible = true;
        }

        private void button35_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox33.Text);
        }

        private void button36_Click(object sender, EventArgs e)
        {
            checkBox6.Checked = true;
            textBox32.Text = Clipboard.GetText(); 
        }

        private void button38_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = GetDirectoryName();
            openFileDialog1.Title = "Browse FANN Data column files";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            openFileDialog1.DefaultExt = "data";
            openFileDialog1.Filter = "Data files (*.data)|*.data|Train files (*.train)|*.train|Train column files (*.ctrain)|*.ctrain|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 3;
            //openFileDialog1.RestoreDirectory = true;

            //openFileDialog1.ReadOnlyChecked = true;
            //openFileDialog1.ShowReadOnly = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox37.Text = openFileDialog1.FileName;
            }
        }

        private void button39_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            if (textBox37.Text == "")
            {
                button38_Click(sender, e);
            }
            string DataPath = textBox37.Text;
            uint epoch = Convert.ToUInt32(textBox1.Text);
            bool DefaultTrainParams = checkBox1.Checked;
            bool ScaleInput = checkBox2.Checked;
            if (ScaleInput) button11_Click(sender, e);
            button10_Click(sender, e);
            //FANN.LoadTrainListColumns(DataPath);
            toolStripStatusLabelMSENum.Text = FANN.TrainOnDataColumns(DataPath, epoch, ScaleInput, DefaultTrainParams).ToString();

            DateTime DT = DateTimeOffset.Now.LocalDateTime;
            textBoxLog.AppendText(DT.ToString() + FANN.LogResult + "\r\n");
        }

        private void button40_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            string DataPathTrainColumn = textBox37.Text;
            string DataPath = textBox36.Text;
            FANN.ConvertTrainListColumnsToTrainData(DataPathTrainColumn, DataPath, false);
        }

        private void button37_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = GetDirectoryName();
            saveFileDialog1.Title = "Save Converted Train Column to Data File";

            saveFileDialog1.CheckPathExists = true;

            saveFileDialog1.DefaultExt = "net";
            saveFileDialog1.Filter = "Data files (*.data)|*.data|Train files (*.train)|*.train|Train column files (*.ctrain)|*.ctrain|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            //saveFileDialog1.RestoreDirectory = true;

            //saveFileDialog1.ReadOnlyChecked = true;
            //saveFileDialog1.ShowReadOnly = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox36.Text = saveFileDialog1.FileName;
            }
        }

        private void button41_Click(object sender, EventArgs e)
        {
            if (FANN == null) button1_Click(sender, e); //Create Neuronet
            string DataPathTrainColumn = textBox38.Text;
            //string DataPath = textBox36.Text;
            FANN.SaveTrainToColumns(DataPathTrainColumn, false);
        }

        private void button42_Click(object sender, EventArgs e)
        {
            saveFileDialog1.InitialDirectory = GetDirectoryName();
            saveFileDialog1.Title = "Save Train Column File";

            saveFileDialog1.CheckPathExists = true;

            saveFileDialog1.DefaultExt = "net";
            saveFileDialog1.Filter = "Data files (*.data)|*.data|Train files (*.train)|*.train|Train column files (*.ctrain)|*.ctrain|Txt files (*.txt)|*.txt|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 1;
            //saveFileDialog1.RestoreDirectory = true;

            //saveFileDialog1.ReadOnlyChecked = true;
            //saveFileDialog1.ShowReadOnly = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox38.Text = saveFileDialog1.FileName;
            }
        }
    }
}

