using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using FANNCSharp;
using FANNCSharp.Double;
using DataType = System.Double;
using System.Windows.Forms;
using System.IO;

namespace FANN_WindowsFormsApplication
{
    class FANNClass
    {
        DataType[] calc_out;
        uint num_layers = 3;
        uint num_input = 2;
        uint num_neurons_hidden = 10;
        uint num_output = 1;
        float desired_error = 0;
        uint max_epochs = 1000;
        uint epochs_between_reports = 10;
        public NeuralNet net;
        public NetworkType NetType;
        public TrainingData TrainData;
        int decimal_point;
        public float ScaleNewInputMin = 0;
        public float ScaleNewInputMax = 1;
        public float ScaleNewOutputMin = 0;
        public float ScaleNewOutputMax = 1;
        public string LogResult;
        public List<double[]> inputList;
        public List<double[]> outputList;

        public FANNClass()
        {
        }

        /// <summary>
        /// Создание нейросети из файла для тренировки 
        /// </summary>
        /// <param name="DataFilePath">Файл тренировки нейросети формата ".data"</param>
        /// <param name="NetLayerType">Тип сети: true = LAYER, false = SHORTCUT</param>
        /// <param name="num_layers">Количество слоёв</param>
        /// <param name="num_neurons_hidden">Количество нейронов в скрытом слое</param>
        public FANNClass(string DataFilePath, bool NetLayerType, uint num_layers, uint num_neurons_hidden)
        {
            this.TrainData = new TrainingData(DataFilePath);
            SetNetParams(NetLayerType, num_layers, this.TrainData.InputCount, num_neurons_hidden, this.TrainData.OutputCount);
            this.net = new NeuralNet(this.NetType, this.num_layers, this.num_input, this.num_neurons_hidden, this.num_output);
            TrainDataInitialize();
        }

        public void Dispose() //Delete
        {
            net.Dispose();
        }

        /// <summary>
        /// Создание нейросети
        /// </summary>
        /// <param name="NetLayerType">Тип сети: true = LAYER, false = SHORTCUT</param>
        /// <param name="num_layers">Количество слоёв</param>
        /// <param name="num_input">Количество входов</param>
        /// <param name="num_neurons_hidden">Количество нейронов в скрытом слое</param>
        /// <param name="num_output">Количество выходов</param>
        public FANNClass(bool NetLayerType, uint num_layers, uint num_input, uint num_neurons_hidden, uint num_output)
        {
            SetNetParams(NetLayerType, num_layers, num_input, num_neurons_hidden, num_output);
            this.net = new NeuralNet(this.NetType, this.num_layers, this.num_input, this.num_neurons_hidden, this.num_output);
            TrainDataInitialize();
        }

        public FANNClass(bool NetLayerType, ICollection<uint> layers)
        {
            this.NetType = NetLayerType ? NetworkType.LAYER : NetworkType.SHORTCUT;
            this.net = new NeuralNet(this.NetType, layers);
            TrainDataInitialize();
        }

        public FANNClass(bool NetLayerType, string layers)
        {
            this.NetType = NetLayerType ? NetworkType.LAYER : NetworkType.SHORTCUT;
            this.net = new NeuralNet(this.NetType, GetListFromText(layers));
            TrainDataInitialize();
        }

        private void SetNetParams(bool NetLayerType, uint num_layers, uint num_input, uint num_neurons_hidden, uint num_output)
        {
            this.NetType = NetLayerType ? NetworkType.LAYER : NetworkType.SHORTCUT;
            this.num_layers = num_layers;
            this.num_input = num_input;
            this.num_neurons_hidden = num_neurons_hidden;
            this.num_output = num_output;
        }

        /// <summary>
        /// Установка параметров для тренировки нейросети
        /// </summary>
        /// <param name="ActivationFunctionHidden">Активационная функция скрытого слоя</param>
        /// <param name="ActivationFunctionOutput">Активационная функция выходного нейрона</param>
        /// <param name="TrainStopFunction"></param>
        /// <param name="BitFailLimit"></param>
        /// <param name="TrainingAlgorithm">Алгорит для тренировки</param>
        public void SetNetTrainParams(ActivationFunction ActivationFunctionHidden, ActivationFunction ActivationFunctionOutput, StopFunction TrainStopFunction, double BitFailLimit, TrainingAlgorithm TrainingAlgorithm)
        {
            try
            {
                this.net.ActivationFunctionHidden = ActivationFunctionHidden;//ActivationFunction.SIGMOID_SYMMETRIC;
                this.net.ActivationFunctionOutput = ActivationFunctionOutput;//ActivationFunction.SIGMOID_SYMMETRIC;
                this.net.TrainStopFunction = TrainStopFunction;//StopFunction.STOPFUNC_BIT;
                this.net.BitFailLimit = BitFailLimit;//0.01F;
                this.net.TrainingAlgorithm = TrainingAlgorithm;// TrainingAlgorithm.TRAIN_RPROP;
            }
            catch { Debug.WriteLine("Ошибка при установке параметров тренировки нейросети. Возможно, нейросеть ещё не создана."); }
            
        }

        public void SetScalingParamsValues(float ScaleNewInputMin, float ScaleNewInputMax, float ScaleNewOutputMin, float ScaleNewOutputMax)
        {
            this.ScaleNewInputMin = ScaleNewInputMin;
            this.ScaleNewInputMax = ScaleNewInputMax;
            this.ScaleNewOutputMin = ScaleNewOutputMin;
            this.ScaleNewOutputMax = ScaleNewOutputMax;
        }

        private void SetScaling(TrainingData TrainData)
        {
            net.SetScalingParams(this.TrainData, this.ScaleNewInputMin, this.ScaleNewInputMax, this.ScaleNewOutputMin, this.ScaleNewOutputMax);
            net.ScaleTrain(this.TrainData);
        }

        private void TrainDataInitialize()
        {
            this.inputList = new List<double[]>();
            this.outputList = new List<double[]>();
        }
        
        /// <summary>
        /// Тренировка нейросети на основе файла
        /// </summary>
        /// <param name="TrainDataFilePath">Путь к файлу с данными для тренировки</param>
        /// <param name="max_epochs">Максимальное количество эпох</param>
        /// <param name="ScaleInput">true = включено масштабирование от 0 до 1, false - исходные данные</param>
        /// <param name="DefaultTrainParams">true = Заполнение параметров тренировки сети по умолачнию</param>
        /// <returns></returns>
        public float TrainOnData(string TrainDataFilePath, uint max_epochs, bool ScaleInput, bool DefaultTrainParams)
        {
            if (TrainDataFilePath == "") return 0;
            this.TrainData = new TrainingData(TrainDataFilePath);
            return TrainOnData(this.TrainData, max_epochs, ScaleInput, DefaultTrainParams);
        }

        public float TrainOnDataColumns(string TrainDataFilePath, uint max_epochs, bool ScaleInput, bool DefaultTrainParams)
        {
            if (TrainDataFilePath == "") return 0;
            LoadTrainListColumns(TrainDataFilePath);
            //this.TrainData = new TrainingData(TrainDataFilePath);
            return TrainOnData(this.TrainData, max_epochs, ScaleInput, DefaultTrainParams);
        }

        public float TrainOnData(TrainingData TrainData, uint max_epochs, bool ScaleInput, bool DefaultTrainParams)
        {
            this.TrainData = new TrainingData(TrainData);
            this.max_epochs = max_epochs;
            if (DefaultTrainParams)
            {
                SetNetTrainParams(ActivationFunction.SIGMOID_SYMMETRIC, ActivationFunction.SIGMOID_SYMMETRIC, StopFunction.STOPFUNC_BIT, 0.01F, TrainingAlgorithm.TRAIN_RPROP);
            }

            net.InitWeights(this.TrainData);
            if (ScaleInput) SetScaling(this.TrainData);

            Debug.WriteLine("Training network on data.");
            try
            {
                net.TrainOnData(this.TrainData, max_epochs, epochs_between_reports, desired_error);
            }
            catch { Debug.WriteLine("Ошибка при тренировке нейросети на файле данных."); }

            DataType[][] input = this.TrainData.Input;
            DataType[][] output = this.TrainData.Output;

            string LogStr = "\r\nNetwork TrainOnData test [ ";
            LogResult = LogStr;
            for (int i = 0; i < this.TrainData.TrainDataLength; i++)
            {
                calc_out = net.Run(input[i]);
                for (int j = 0; j < input[i].Length; j++) //Add input
                {
                    LogResult += input[i][j].ToString() + "; ";
                }
                LogResult += "] -> [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet output
                {
                    LogResult += calc_out[j].ToString() + "; ";
                }
                LogResult += "], should be [ ";
                for (int j = 0; j < output[i].Length; j++) //Add real output
                {
                    LogResult += output[i][j].ToString() + "; ";
                }
                LogResult += "], difference = [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet and output difference
                {
                    LogResult += (Math.Abs(calc_out[j] - output[i][j])).ToString() + "; ";
                }
                LogResult += "]" + LogStr;
            }
            LogResult += "MSE=" + net.MSE.ToString() + " ]";
            Debug.WriteLine(LogResult);
            return net.MSE;
        }

        public float TrainOnDataEpoch(TrainingData TrainData, int trainCount, bool ScaleInput, bool DefaultTrainParams)
        {
            this.TrainData = TrainData;
            if (DefaultTrainParams)
            {
                SetNetTrainParams(ActivationFunction.SIGMOID_SYMMETRIC, ActivationFunction.SIGMOID_SYMMETRIC, StopFunction.STOPFUNC_BIT, 0.01F, TrainingAlgorithm.TRAIN_RPROP);
            }

            net.InitWeights(this.TrainData);
            if (ScaleInput) SetScaling(this.TrainData);

            DataType[][] input = this.TrainData.Input;
            DataType[][] output = this.TrainData.Output;

            Debug.WriteLine("Training network epoch on data.");
            for (int i = 0; i < trainCount; i++)
            {
                net.TrainEpoch(this.TrainData);
                //Debug.WriteLine("Iteration: {0}, MSE: {1}", i + 1, net.MSE);
            }

            string LogStr = "\r\nNetwork TrainOnDataEpoch test [ ";
            LogResult = LogStr;
            for (int i = 0; i < this.TrainData.TrainDataLength; i++)
            {
                calc_out = net.Run(input[i]);
                for (int j = 0; j < input[i].Length; j++) //Add input
                {
                    LogResult += input[i][j].ToString() + "; ";
                }
                LogResult += "] -> [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet output
                {
                    LogResult += calc_out[j].ToString() + "; ";
                }
                LogResult += "], should be [ ";
                for (int j = 0; j < output[i].Length; j++) //Add real output
                {
                    LogResult += output[i][j].ToString() + "; ";
                }
                LogResult += "], difference = [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet and output difference
                {
                    LogResult += (Math.Abs(calc_out[j] - output[i][j])).ToString() + "; ";
                }
                LogResult += "]" + LogStr;
            }
            LogResult += "MSE=" + net.MSE.ToString() + " ]";
            Debug.WriteLine(LogResult);
            return net.MSE;
        }

        /// <summary>
        /// Тренировка сети на основе файла и заданного количества эпох, учитывая предыдущий опыт текущего вызова
        /// </summary>
        /// <param name="TrainDataFilePath">Путь к файлу с данными для тренировки</param>
        /// <param name="trainCount">Количество тренировок</param>
        /// <param name="ScaleInput">true = включено масштабирование от 0 до 1, false - исходные данные</param>
        /// <param name="DefaultTrainParams">true = Заполнение параметров тренировки сети по умолачнию</param>
        /// <returns></returns>
        public float TrainOnDataEpoch(string TrainDataFilePath, int trainCount, bool ScaleInput, bool DefaultTrainParams)
        {
            this.TrainData = new TrainingData(TrainDataFilePath);
            return TrainOnDataEpoch(this.TrainData, trainCount, ScaleInput, DefaultTrainParams);
        }

        /// <summary>
        /// Тренировать сеть на основе массивов double[]
        /// </summary>
        /// <param name="input">Входной массив данных</param>
        /// <param name="output">Выходной массив данных</param>
        /// <param name="trainCount">Количество тренировок (циклов)</param>
        /// <param name="DefaultTrainParams">true = Заполнение параметров тренировки сети по умолачнию</param>
        /// <returns></returns>
        public float TrainOnIO(double[] input, double[] output, int trainCount, bool DefaultTrainParams)
        {
            if (DefaultTrainParams)
            {
                SetNetTrainParams(ActivationFunction.SIGMOID_SYMMETRIC, ActivationFunction.SIGMOID_SYMMETRIC, StopFunction.STOPFUNC_BIT, 0.01F, TrainingAlgorithm.TRAIN_RPROP);
            }

            Debug.WriteLine("Training network on IO[].");
            for (int i = 0; i < trainCount; i++)
            {
                net.Train(input, output);
            }

            string LogStr = "\r\nNetwork TrainOnIO[] test [ ";
            LogResult = LogStr;
            calc_out = net.Run(input);
            for (int j = 0; j < input.Length; j++) //Add input
            {
                LogResult += input[j].ToString() + "; ";
            }
            LogResult += "] -> [ ";
            for (int j = 0; j < calc_out.Length; j++) //Add neuronet output
            {
                LogResult += calc_out[j].ToString() + "; ";
            }
            LogResult += "], should be [ ";
            for (int j = 0; j < output.Length; j++) //Add real output
            {
                LogResult += output[j].ToString() + "; ";
            }
            LogResult += "], difference = [ ";
            for (int j = 0; j < calc_out.Length; j++) //Add neuronet and output difference
            {
                LogResult += (Math.Abs(calc_out[j] - output[j])).ToString() + "; ";
            }
            LogResult += "]" + LogStr;
            LogResult += "MSE=" + net.MSE.ToString() + " ]";
            Debug.WriteLine(LogResult);
            return net.MSE;
        }

        public float TrainOnIO(double[][] input, double[][] output, int trainCount, bool ScaleInput, bool DefaultTrainParams)
        {
            if (DefaultTrainParams)
            {
                SetNetTrainParams(ActivationFunction.SIGMOID_SYMMETRIC, ActivationFunction.SIGMOID_SYMMETRIC, StopFunction.STOPFUNC_BIT, 0.01F, TrainingAlgorithm.TRAIN_RPROP);
            }

            this.TrainData = new TrainingData();
            this.TrainData.SetTrainData(input, output);

            net.InitWeights(this.TrainData);
            if (ScaleInput) SetScaling(this.TrainData);

            DataType[][] input2 = this.TrainData.Input;
            DataType[][] output2 = this.TrainData.Output;

            Debug.WriteLine("Training network on IO[][].");
            for (int i = 0; i < trainCount; i++)
            {
                net.TrainEpoch(this.TrainData);
                //Debug.WriteLine("Iteration: {0}, MSE: {1}", i + 1, net.MSE);
            }

            string LogStr = "\r\nNetwork TrainOnIO test [ ";
            LogResult = LogStr;
            for (int i = 0; i < this.TrainData.TrainDataLength; i++)
            {
                calc_out = net.Run(input2[i]);
                for (int j = 0; j < input2[i].Length; j++) //Add input
                {
                    LogResult += input2[i][j].ToString() + "; ";
                }
                LogResult += "] -> [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet output
                {
                    LogResult += calc_out[j].ToString() + "; ";
                }
                LogResult += "], should be [ ";
                for (int j = 0; j < output2[i].Length; j++) //Add real output
                {
                    LogResult += output2[i][j].ToString() + "; ";
                }
                LogResult += "], difference = [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet and output difference
                {
                    LogResult += (Math.Abs(calc_out[j] - output2[i][j])).ToString() + "; ";
                }
                LogResult += "]" + LogStr;
            }
            LogResult += "MSE=" + net.MSE.ToString() + " ]";
            Debug.WriteLine(LogResult);
            return net.MSE;
        }

        private List<uint> GetListFromText(string Text)
        {
            string[] separators = { ",", ".", ";", "#" };
            string value = Text;
            string[] parts = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            List<uint> layers = new List<uint>();
            for (int i = 0; i < parts.Length; i++)
            {
                layers.Add(Convert.ToUInt32(parts[i]));
            }
            return layers;
        }

        private double[] GetDoubleFromText(string Text)
        {
            string[] separators = { ",", ".", ";", "#" };
            string inputString = Text;
            string[] parts = inputString.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            double[] result = new double[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                result[i] = Convert.ToDouble(parts[i]);
            }
            return result;
        }

        public int AddTrainIOToList(string inputText, string outputText)
        {
            double[] input = GetDoubleFromText(inputText);
            double[] output = GetDoubleFromText(outputText);
            return AddTrainIOToList(input, output);
        }

        public int AddTrainIOToList(double[] input, double[] output)
        {
            this.inputList.Add(input);
            this.outputList.Add(output);
            return this.inputList.Count;
        }

        private float[] Double1dToFloat1d(double[] inputD)
        {
            float[] outputD = new float[inputD.Length];
            for (int i = 0; i < outputD.Length; i++) { outputD[i] = (float)inputD[i]; }
            return outputD;
        }
        private double[] Float1dToDouble1d(float[] inputD)
        {
            double[] outputD = new double[inputD.Length];
            for (int i = 0; i < outputD.Length; i++) { outputD[i] = (double)inputD[i]; }
            return outputD;
        }

        private double[][] GetDouble2dFromList1d(List<double[]> List)
        {
            double[][] D2d = new double[List.Count][];
            for (int x = 0; x < D2d.Length; x++)
            {
                D2d[x] = new double[List[x].Length];
                D2d[x] = List[x];
            }
            return D2d;
        }

        public float TrainOnIOList(int trainCount, bool ScaleInput, bool DefaultTrainParams)
        {
            if (this.inputList.Count == 0 || this.outputList.Count == 0) return 0;
            double[][] input = GetDouble2dFromList1d(this.inputList);
            double[][] output = GetDouble2dFromList1d(this.outputList);
            return TrainOnIO(input, output, trainCount, ScaleInput, DefaultTrainParams);
        }

        public double[] RunNetOnData(double[] input, bool ScaleInput)
        {
            this.TrainData = new TrainingData();
            double[][] input2 = new double[1][];
            input2[0] = input;
            double[][] output2 = input2;

            this.TrainData.SetTrainData(input2, output2);
            string LogStr = "\r\nNetwork RunNetOnData input run [ ";
            LogResult = LogStr;
            net.ResetMSE();
            if (ScaleInput) net.ScaleInput(this.TrainData.GetTrainInput((uint)0));
            calc_out = net.Run(this.TrainData.GetTrainInput((uint)0));
            if (ScaleInput) net.DescaleOutput(calc_out);
            for (int j = 0; j < input.Length; j++) //Add input
            {
                LogResult += input[j].ToString() + "; ";
            }
            LogResult += "] -> [ ";
            for (int j = 0; j < calc_out.Length; j++) //Add neuronet output
            {
                LogResult += calc_out[j].ToString() + "; ";
            }
            LogResult += " ]";
            Debug.WriteLine(LogResult);
            return calc_out;
        }

        public double[] RunNetOnData(double[] input, double[] output, bool ScaleInput)
        {
            this.TrainData = new TrainingData();
            double[][] input2 = new double[1][];
            input2[0] = input;
            double[][] output2 = new double[1][];
            output2[0] = output;

            this.TrainData.SetTrainData(input2, output2);
            return RunNetOnData(this.TrainData, ScaleInput);
        }

        /// <summary>
        /// Выполнить нейросеть на основе данных
        /// </summary>
        /// <param name="TrainData">Переменная с данными</param>
        /// <param name="ScaleInput">true = включено масштабирование от 0 до 1, false - исходные данные</param>
        /// <returns></returns>
        public double[] RunNetOnData(TrainingData TrainData, bool ScaleInput)
        {
            this.TrainData = TrainData;
            string LogStr = "\r\nNetwork RunNetOnData run [ ";
            LogResult = LogStr;
            for (int i = 0; i < this.TrainData.TrainDataLength; i++)
            {
                net.ResetMSE();
                if (ScaleInput) net.ScaleInput(this.TrainData.GetTrainInput((uint)i));
                calc_out = net.Run(this.TrainData.GetTrainInput((uint)i));
                if (ScaleInput) net.DescaleOutput(calc_out);

                for (int j = 0; j < this.TrainData.InputAccessor[i].Count; j++) //Add input
                {
                    LogResult += this.TrainData.InputAccessor[i][j].ToString() + "; ";
                }
                LogResult += "] -> [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet output
                {
                    LogResult += calc_out[j].ToString() + "; ";
                }
                LogResult += "], should be [ ";
                for (int j = 0; j < this.TrainData.OutputAccessor[i].Count; j++) //Add real output
                {
                    LogResult += this.TrainData.OutputAccessor[i][j].ToString() + "; ";
                }
                LogResult += "], difference = [ ";
                for (int j = 0; j < calc_out.Length; j++) //Add neuronet and output difference
                {
                    LogResult += (Math.Abs(calc_out[j] - this.TrainData.OutputAccessor[i][j])).ToString() + "; ";
                }
                LogResult += "]" + LogStr;
            }
                LogResult += "MSE=" + net.MSE.ToString() + " ]";
                Debug.WriteLine(LogResult);
                return calc_out;
        }

        public double[] RunNetOnData(string TrainDataFilePath, bool ScaleInput)
        {
            this.TrainData = new TrainingData(TrainDataFilePath);
            return RunNetOnData(this.TrainData, ScaleInput);
        }

        public double[] RunNetOnData(double[][] input, double[][] output, bool ScaleInput)
        {
            this.TrainData = new TrainingData();
            this.TrainData.SetTrainData(input, output);
            return RunNetOnData(this.TrainData, ScaleInput);
        }

        public double[] RunNetOnData(bool ScaleInput)
        {
            return RunNetOnData(this.TrainData, ScaleInput);
        }

        public float TestNetOnData(TrainingData TrainData, bool ScaleInput)
        {
            this.TrainData = TrainData; 
            if (ScaleInput) SetScaling(this.TrainData);
            float Test = net.TestData(this.TrainData);
            Console.WriteLine("Test on data result {0}: ", Test);
            return Test; 
        }

        public float TestNetOnData(string TrainDataFilePath, bool ScaleInput)
        {
            if (TrainDataFilePath == "") return 0;
            this.TrainData = new TrainingData(TrainDataFilePath);
            return TestNetOnData(this.TrainData, ScaleInput);
        }

        public float TestNetOnData(bool ScaleInput)
        {
            float Test = net.TestData(this.TrainData);
            Console.WriteLine("Test on data result {0}: ", Test);
            return Test;
        }

        public float TestNetOnIO(double[] input, double[] output)
        {
            Debug.WriteLine("Test network on IO[].");
            DataType[] Test = new DataType[1];
            Test = net.Test(input, output);
            return (float)Test[0];
        }

        public float TestNetOnIO(double[][] input, double[][] output, bool ScaleInput)
        {
            Debug.WriteLine("Test network on IO[][].");
            this.TrainData = new TrainingData();
            this.TrainData.SetTrainData(input, output);
            return TestNetOnData(this.TrainData, ScaleInput);
        }

        public float TestNetOnIO(bool ScaleInput)
        {
            Debug.WriteLine("Test network on IO[][].");
            return TestNetOnData(this.TrainData, ScaleInput);
        }

        public void SaveNet(string NetFilePath, bool SaveNetToFixed)
        {
            Debug.WriteLine("Saving network.");
            net.Save(NetFilePath);
            if (SaveNetToFixed)
            {
                decimal_point = net.SaveToFixed(NetFilePath + "_");
            }
        }

        public void SaveTrain(string TrainFilePath, bool SaveTrainToFixed)
        {
            Debug.WriteLine("Saving train.");
            this.TrainData.SaveTrain(TrainFilePath);
            if (SaveTrainToFixed)
            {
                this.TrainData.SaveTrainToFixed(TrainFilePath + "_", (uint)decimal_point);
            }
        }

        public void SaveTrainToColumns(string TrainColumnFilePath, bool SaveTrainToFixed)
        {
            Debug.WriteLine("Saving Train To Columns.");
            string FileStr = string.Empty;

            //Handles
            for (int j = 0; j < this.TrainData.Input[0].Length; j++)
            {
                FileStr += "Input " + (j + 1).ToString() + "\t";
            }
            for (int k = 0; k < this.TrainData.Output[0].Length; k++)
            {
                FileStr += "Output " + (k + 1).ToString() + "\t";
            }
            FileStr += "\r\n";

            for (int i = 0; i < this.TrainData.Input.Length; i++)
            {
                for (int j = 0; j < this.TrainData.Input[i].Length; j++)
                {
                    FileStr += this.TrainData.Input[i][j].ToString() + "\t";
                }
                for (int k = 0; k < this.TrainData.Output[i].Length; k++)
                {
                    FileStr += this.TrainData.Output[i][k].ToString() + "\t";
                }
                FileStr += "\r\n";
            }
            this.LogResult = FileStr;
            File_WriteAllText(TrainColumnFilePath, FileStr);
        }

        public void SaveTrainIOList(string TrainFilePath, bool ScaleInput, bool SaveTrainToFixed)
        {
            Debug.WriteLine("Saving train IO list.");
            TrainingData BackupTD = this.TrainData;
            this.TrainData = new TrainingData();
            double[][] input = GetDouble2dFromList1d(this.inputList);
            double[][] output = GetDouble2dFromList1d(this.outputList);
            this.TrainData.SetTrainData(input, output);
            if (ScaleInput) SetScaling(this.TrainData);

            this.TrainData.SaveTrain(TrainFilePath);
            if (SaveTrainToFixed)
            {
                this.TrainData.SaveTrainToFixed(TrainFilePath + "_", (uint)decimal_point);
            }
            this.TrainData = BackupTD;
        }

        public void CheckDirectory(string FilePath)
        {
            string Dir = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }
        }

        public void File_WriteAllText(string FilePath, string FileText)
        {
            //CheckDirectory(FilePath);
            System.IO.File.WriteAllText(FilePath, FileText);
        }

        public void SaveTrainIOListToColumns(string TrainFilePath)
        {
            Debug.WriteLine("Saving train IO list to columns.");
            string FileStr = string.Empty;
            
            //Handles
            for (int j = 0; j < this.inputList[0].Length; j++)
            {
                FileStr += "Input " + (j + 1).ToString() + "\t";
            }
            for (int j = 0; j < this.inputList[0].Length; j++)
            {
                FileStr += "Input " + (j+1).ToString() + "\t";
            }
            for (int k = 0; k < this.outputList[0].Length; k++)
            {
                FileStr += "Output " + (k+1).ToString() + "\t";
            }
            FileStr += "\r\n";

            for (int i = 0; i < this.inputList.Count; i++)
            {
                for (int j = 0; j < this.inputList[i].Length; j++)
                {
                    FileStr += this.inputList[i][j].ToString() + "\t";
                }
                for (int k = 0; k < this.outputList[i].Length; k++)
                {
                    FileStr += this.outputList[i][k].ToString() + "\t";
                }
                FileStr += "\r\n";
            }
            this.LogResult = FileStr;
            File_WriteAllText(TrainFilePath, FileStr);
        }

        public void LoadTrainListColumns(string TrainColumnsFilePath)
        {
            Debug.WriteLine("Loading train list columns.");
            string FileStr = string.Empty;
            double[][] input;
            double[][] output;
            TrainingData TrainData = new TrainingData();
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(TrainColumnsFilePath))
                {
                    FileStr = sr.ReadToEnd();
                }
                //Handles
                string[] Lines = FileStr.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                string[] handles = Lines[0].Split('\t');

                string[] LineStr;
                input = new double[Lines.Length-2][];
                output = new double[Lines.Length - 2][];
                int inputCount = 0;
                for (int k = 0; k < handles.Length-1; k++)
                {
                    if (handles[k].IndexOf("Input ") != -1 )
                    {
                        inputCount++;
                    }
                }
                int inpInd = 0;
                int outInd = 0;
                for (int j = 0; j < Lines.Length-2; j++)
                {
                    LineStr = Lines[j+1].Split('\t');
                    input[j] = new double[inputCount];
                    output[j] = new double[handles.Length - inputCount];
                    inpInd = 0;
                    outInd = 0;
                    for (int k = 0; k < LineStr.Length; k++)
                    {
                        if (handles[k].IndexOf("Input ") != -1)
                        {
                            input[j][inpInd] = double.Parse(LineStr[k]);
                            inpInd++;
                        }
                        else
                        {
                            output[j][outInd] = double.Parse(LineStr[k]);
                            outInd++;
                        }
                    }
                }
                TrainData.SetTrainData(input, output);
                this.TrainData = TrainData;
            }
            catch { Debug.WriteLine("File not found."); };
            //File_WriteAllText(TrainFilePath, FileStr);
        }

        public void ConvertTrainListColumnsToTrainData(string TrainColumnFilePath, string ExportTrainFilePath, bool SaveTrainToFixed)
        {
            LoadTrainListColumns(TrainColumnFilePath);
            SaveTrain(ExportTrainFilePath, SaveTrainToFixed);
        }

        public void LoadNet(string NetFilePath) 
        {
            Debug.WriteLine("Load network: " + NetFilePath);
            net = new NeuralNet(NetFilePath);
            net.PrintConnections();
            net.PrintParameters();
            //TrainingData TrainData = new TrainingData("xor.data");
            //RunNetOnData(TrainData, true);
        }

        public double double_rand(Random rnd, double min, double max)
        {
            //Random rnd = new Random((int)DateTime.Now.Ticks);
            //Random rnd = new Random();
            double scale = rnd.NextDouble();    /* [0, 1.0] */
            return min + scale * (max - min);   /* [min, max] */
        }

        public double[] GetNetResultInput(double[] outputResult, double error, int maxIterations, double rndMin, double rndMax, bool ScaleInput)
        {
            Random rnd = new Random();
            double[] input = new double[net.InputCount];
            double[] output = new double[net.OutputCount];
            double[] error_ = new double[output.Length];
            double errorAverage;
            List<double[]> inputList = new List<double[]>();
            List<double> errorList = new List<double>();
            DataType[] net_out; 
            TrainData = new TrainingData();
            int iter = 0;

            //double[] input = { -1, -1 };

            for (int i = 0; i < input.Length; i++)
            {
                input[i] = double_rand(rnd, rndMin, rndMax);
            }
            inputList.Add(input);
            TrainData.SetTrainData(1, input, outputResult);
            net.ResetMSE();
            if (ScaleInput) net.ScaleInput(TrainData.GetTrainInput((uint)0));
            net_out = net.Run(TrainData.GetTrainInput((uint)0));
            if (ScaleInput) net.DescaleOutput(net_out);
            for (int i = 0; i < output.Length; i++)
            {
                error_[i] = Math.Abs(net_out[i] - outputResult[i]);
            }
            errorAverage = error_.Average();

            inputList.Add(input);
            errorList.Add(errorAverage);
            while (errorAverage >= error && (iter < maxIterations))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    input[i] = double_rand(rnd, rndMin, rndMax);
                }
                
                TrainData.SetTrainData(1, input, outputResult);
                net.ResetMSE();
                if (ScaleInput) net.ScaleInput(TrainData.GetTrainInput((uint)0));
                net_out = net.Run(TrainData.GetTrainInput((uint)0));
                if (ScaleInput) net.DescaleOutput(net_out);
                for (int i = 0; i < output.Length; i++)
                {
                    error_[i] = Math.Abs(net_out[i] - outputResult[i]);
                }
                errorAverage = error_.Average();

                inputList.Add(input);
                errorList.Add(errorAverage);
                iter++;
            }
            int bestError = errorList.IndexOf(errorList.Min()); //Get index at min error
            input = inputList[bestError]; //Set input value from list with min error
            errorAverage = errorList.Min();

            string LogStr = "\r\nNetwork GetNetResult [ ";
            LogResult = LogStr;
            for (int j = 0; j < outputResult.Length; j++) //Add input
            {
                LogResult += outputResult[j].ToString() + "; ";
            }
            LogResult += "] ( ";
            for (int j = 0; j < net_out.Length; j++) //Add input
            {
                LogResult += net_out[j].ToString() + "; ";
            }
            LogResult += ") -> [ ";
            for (int j = 0; j < input.Length; j++) //Add input
            {
                LogResult += input[j].ToString() + "; ";
            }
            LogResult += "], error " + errorAverage.ToString() + " iteration=" + iter.ToString();
            Debug.WriteLine(LogResult);
            return input;
        }

        public double[] GetNetResultInput(double[] outputResult, double error, int maxIterations, double[] rndMin, double[] rndMax, bool ScaleInput)
        {
            Random rnd = new Random();
            double[] input = new double[net.InputCount];
            double[] output = new double[net.OutputCount];
            double[] error_ = new double[output.Length];
            double errorAverage;
            List<double[]> inputList = new List<double[]>();
            List<double> errorList = new List<double>();
            DataType[] net_out;
            TrainData = new TrainingData();
            int iter = 0;

            //double[] input = { -1, -1 };
            if (input.Length != rndMin.Length) return null;
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = double_rand(rnd, rndMin[i], rndMax[i]);
            }
            inputList.Add(input);
            TrainData.SetTrainData(1, input, outputResult);
            net.ResetMSE();
            if (ScaleInput) net.ScaleInput(TrainData.GetTrainInput((uint)0));
            net_out = net.Run(TrainData.GetTrainInput((uint)0));
            if (ScaleInput) net.DescaleOutput(net_out);
            for (int i = 0; i < output.Length; i++)
            {
                error_[i] = Math.Abs(net_out[i] - outputResult[i]);
            }
            errorAverage = error_.Average();

            inputList.Add(input);
            errorList.Add(errorAverage);
            while (errorAverage >= error && (iter < maxIterations))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    input[i] = double_rand(rnd, rndMin[i], rndMax[i]);
                }

                TrainData.SetTrainData(1, input, outputResult);
                net.ResetMSE();
                if (ScaleInput) net.ScaleInput(TrainData.GetTrainInput((uint)0));
                net_out = net.Run(TrainData.GetTrainInput((uint)0));
                if (ScaleInput) net.DescaleOutput(net_out);
                for (int i = 0; i < output.Length; i++)
                {
                    error_[i] = Math.Abs(net_out[i] - outputResult[i]);
                }
                errorAverage = error_.Average();

                inputList.Add(input);
                errorList.Add(errorAverage);
                iter++;
            }
            int bestError = errorList.IndexOf(errorList.Min()); //Get index at min error
            input = inputList[bestError]; //Set input value from list with min error
            errorAverage = errorList.Min();

            string LogStr = "\r\nNetwork GetNetResult [ ";
            LogResult = LogStr;
            for (int j = 0; j < outputResult.Length; j++) //Add input
            {
                LogResult += outputResult[j].ToString() + "; ";
            }
            LogResult += "] ( ";
            for (int j = 0; j < net_out.Length; j++) //Add input
            {
                LogResult += net_out[j].ToString() + "; ";
            }
            LogResult += ") -> [ ";
            for (int j = 0; j < input.Length; j++) //Add input
            {
                LogResult += input[j].ToString() + "; ";
            }
            LogResult += "], error " + errorAverage.ToString() + " iteration=" + iter.ToString();
            Debug.WriteLine(LogResult);
            return input;
        }

        public delegate string FunkGetNetResultInput(double[] input);

        public double[] GetNetResultInput(FunkGetNetResultInput funcPredict, int InputCount, int OutputCount, double[] outputResult, double error, int maxIterations, double rndMin, double rndMax)
        {
            Random rnd = new Random();
            double[] input = new double[InputCount];
            double[] output = new double[OutputCount];
            double[] error_ = new double[output.Length];
            double errorAverage;
            List<double[]> inputList = new List<double[]>();
            List<double> errorList = new List<double>();
            DataType[] net_out = new DataType[OutputCount];
            TrainData = new TrainingData();
            int iter = 0;

            //double[] input = { -1, -1 };
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = double_rand(rnd, rndMin, rndMax);
            }
            inputList.Add(input);
            for (int i = 0; i < net_out.Length; i++)
            {
                net_out[i] = Convert.ToDouble(funcPredict(input));
            }
            
            for (int i = 0; i < output.Length; i++)
            {
                error_[i] = Math.Abs(net_out[i] - outputResult[i]);
            }
            errorAverage = error_.Average();

            inputList.Add(input);
            errorList.Add(errorAverage);
            while (errorAverage >= error && (iter < maxIterations))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    input[i] = double_rand(rnd, rndMin, rndMax);
                }

                for (int i = 0; i < net_out.Length; i++)
                {
                    net_out[i] = Convert.ToDouble(funcPredict(input));
                }
                for (int i = 0; i < output.Length; i++)
                {
                    error_[i] = Math.Abs(net_out[i] - outputResult[i]);
                }
                errorAverage = error_.Average();

                inputList.Add(input);
                errorList.Add(errorAverage);
                iter++;
            }
            int bestError = errorList.IndexOf(errorList.Min()); //Get index at min error
            input = inputList[bestError]; //Set input value from list with min error
            errorAverage = errorList.Min();

            string LogStr = "\r\nNetwork GetNetResult from Func '" + funcPredict.Method.Name + "' [ ";
            LogResult = LogStr;
            for (int j = 0; j < outputResult.Length; j++) //Add input
            {
                LogResult += outputResult[j].ToString() + "; ";
            }
            LogResult += "] ( ";
            for (int j = 0; j < net_out.Length; j++) //Add input
            {
                LogResult += net_out[j].ToString() + "; ";
            }
            LogResult += ") -> [ ";
            for (int j = 0; j < input.Length; j++) //Add input
            {
                LogResult += input[j].ToString() + "; ";
            }
            LogResult += "], error " + errorAverage.ToString() + " iteration=" + iter.ToString();
            Debug.WriteLine(LogResult);
            return input;
        }

        public double[] GetNetResultInput(FunkGetNetResultInput funcPredict, int InputCount, int OutputCount, double[] outputResult, double error, int maxIterations, double[] rndMin, double[] rndMax)
        {
            Random rnd = new Random();
            double[] input = new double[InputCount];
            double[] output = new double[OutputCount];
            double[] error_ = new double[output.Length]; 
            double errorAverage;
            List<double[]> inputList = new List<double[]>();
            List<double> errorList = new List<double>();
            DataType[] net_out = new DataType[OutputCount];
            TrainData = new TrainingData();
            int iter = 0;

            //double[] input = { -1, -1 };
            if (input.Length != rndMin.Length) return null;
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = double_rand(rnd, rndMin[i], rndMax[i]);
            }
            inputList.Add(input);

            //double[] input = { -1, -1 };
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = double_rand(rnd, rndMin[i], rndMax[i]);
            }
            inputList.Add(input);
            for (int i = 0; i < net_out.Length; i++)
            {
                net_out[i] = Convert.ToDouble(funcPredict(input));
            }

            for (int i = 0; i < output.Length; i++)
            {
                error_[i] = Math.Abs(net_out[i] - outputResult[i]);
            }
            errorAverage = error_.Average();

            inputList.Add(input);
            errorList.Add(errorAverage);
            while (errorAverage >= error && (iter < maxIterations))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    input[i] = double_rand(rnd, rndMin[i], rndMax[i]);
                }

                for (int i = 0; i < net_out.Length; i++)
                {
                    net_out[i] = Convert.ToDouble(funcPredict(input));
                }
                for (int i = 0; i < output.Length; i++)
                {
                    error_[i] = Math.Abs(net_out[i] - outputResult[i]);
                }
                errorAverage = error_.Average();

                inputList.Add(input);
                errorList.Add(errorAverage);
                iter++;
            }
            int bestError = errorList.IndexOf(errorList.Min()); //Get index at min error
            input = inputList[bestError]; //Set input value from list with min error
            errorAverage = errorList.Min();

            string LogStr = "\r\nNetwork GetNetResult from Func '" + funcPredict.ToString() + "' [ ";
            LogResult = LogStr;
            for (int j = 0; j < outputResult.Length; j++) //Add input
            {
                LogResult += outputResult[j].ToString() + "; ";
            }
            LogResult += "] ( ";
            for (int j = 0; j < net_out.Length; j++) //Add input
            {
                LogResult += net_out[j].ToString() + "; ";
            }
            LogResult += ") -> [ ";
            for (int j = 0; j < input.Length; j++) //Add input
            {
                LogResult += input[j].ToString() + "; ";
            }
            LogResult += "], error " + errorAverage.ToString() + " iteration=" + iter.ToString();
            Debug.WriteLine(LogResult);
            return input;
        }

    }
}
