using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace pirometr
{
    class Program
    {
        static bool _continue;
        static SerialPort _serialPort;
        static string stringSRC_LAM_1 = "";
        static string stringSRC_LAM_2 = "";

        static void Main(string[] args)
        {
            string message;
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;

            // Создание нового объекта SerialPort с установками по умолчанию.
            _serialPort = new SerialPort();

            // Позволяем пользователю установить подходящие свойства.
            _serialPort.PortName = "COM4";
            _serialPort.BaudRate = 19200;
            _serialPort.Parity = Parity.Even;
            _serialPort.DataBits = 7;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
            _serialPort.NewLine = "\0";
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(sp_DataReceived);
            

            // Установка таймаутов чтения/записи (read/write timeouts)
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;
            _serialPort.Open();
            _continue = true;

            _serialPort.Write("c6\0"); // запрос доп информации по прибору

            Console.WriteLine("Type QUIT to exit");

            while (_continue)
            {
                message = Console.ReadLine();

                if (stringComparer.Equals("quit", message))
                {
                    _continue = false;
                }
            }
            _serialPort.Close();
        }

        static void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    string data = _serialPort.ReadTo("\0");
                    si_DataReceived(data);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static float ConvertStrHexToFloat(string data)
        {
            uint num = uint.Parse(data, System.Globalization.NumberStyles.AllowHexSpecifier);
            byte[] floatVals = BitConverter.GetBytes(num);

            if (BitConverter.IsLittleEndian)
                floatVals = floatVals.Reverse().ToArray();

            return BitConverter.ToSingle(floatVals, 0);
        }

        /// <summary>
        /// обработка основного сообщения 
        /// </summary>
        /// <param name="data"></param>
        static void MainMSG(string data)
        {
            string stringSRCTemp = "";
            string stringSRCEmissFirst = "";
            string stringSRCEmissSecond = "";

            for (int i = 1; i < 9; i++)
                stringSRCTemp += data[i];

            for (int i = 10; i < 17; i++)
                stringSRCEmissFirst += data[i];

            for (int i = 18; i < 25; i++)
                stringSRCEmissSecond += data[i];

            float tmp = ConvertStrHexToFloat(stringSRCTemp) - 273;
            Console.WriteLine("TEMP=" + tmp.ToString());

            tmp = ConvertStrHexToFloat(stringSRCEmissFirst);
            Console.WriteLine("Emiss_1=" + tmp.ToString());

            tmp = ConvertStrHexToFloat(stringSRCEmissSecond);
            Console.WriteLine("Emiss_2=" + tmp.ToString());

            Console.WriteLine("Lam_1=" + stringSRC_LAM_1.ToString());
            Console.WriteLine("Lam_2=" + stringSRC_LAM_2.ToString());
        }

        /// <summary>
        /// обработчик сообщение Z. статус
        /// </summary>
        /// <param name="data"></param>
        static void Z_MSG(string data)
        {
            switch(data[1]+""+data[2])
            {
                case "01":
                    Console.WriteLine("мало инфракрасного излучения"); 
                    break;
                case "10":
                    Console.WriteLine("мало яркости относительных заданных параметров памяти устройства");
                    break;
            }
        }

        /// <summary>
        /// обработчик сообщение С. доп информация о приборе
        /// </summary>
        /// <param name="data"></param>
        static void C_MSG(string data)
        {
            if (data[1] == '6')
            {
                for (int i = 2; i < 7; i++)
                    stringSRC_LAM_1 += data[i];

                for (int i = 7; i < 12; i++)
                    stringSRC_LAM_2 += data[i];
            }
        }

        //обработчику сообщнией Е. ошибки
        static void E_MSG(string data)
        {
            int numberError = int.Parse(data[1] + "" + data[2]);
            int numberErrorMSG = 0;
            if (data.Length>3)
               numberErrorMSG = int.Parse(data[3] + "" + data[4] +""+ data[5] + "" + data[6]);

            Console.WriteLine("error=" + numberError.ToString());

            switch(numberError)
            {
                case 01:
                case 02:
                case 03:
                case 04:
                case 05:
                case 06:
                    Console.WriteLine("Сообщение от камеры - " + numberErrorMSG.ToString());
                    break;
                case 11:
                    Console.WriteLine("Контроллер не может правильно отслеживать процесс");
                    break;
                case 12:
                    Console.WriteLine("Помехи искажают результат измерения");
                    break;
                case 13:
                    Console.WriteLine("Поступающий сигнал превышает допустимые пределы");
                    break;
                case 50:
                    Console.WriteLine("Нет связи мужду камерой и дисплеем");
                    break;
                case 51:
                case 52:
                case 53:
                    Console.WriteLine("Ошибка идентификации связи при подключении питания");
                    break;
                case 95:
                case 96:
                case 97:
                case 98:
                case 99:
                    Console.WriteLine("нет информации по ошибки");
                    break;
            }
        }

        /// <summary>
        /// разбор сообщения
        /// </summary>
        /// <param name="data"></param>
        static void si_DataReceived(string data)
        {
            Console.Clear();
            switch(data[0])
            {
                case 'z':
                    Z_MSG(data);
                    break;
                case 'b':
                    MainMSG(data);
                    break;
                case 'c':
                    C_MSG(data);
                    break;
                case 'e':
                    E_MSG(data);
                    break;
            }
        }
    }
}
