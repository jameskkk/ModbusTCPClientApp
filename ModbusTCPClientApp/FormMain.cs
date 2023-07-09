using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;

namespace ModbusTCPClientApp
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            // Reference: https://fullstackladder.dev/blog/2022/11/12/modbustcp-client-using-csharp/
            //var request = new byte[]
            //{
            //          0x00, 0x01, // Transaction Identifier、連續數字或隨機數字都可以
            //          0x00, 0x00, // Protocol Identifier、固定 0
            //          0x00, 0x06, // Length、接下來有 6 bytes 的資料
            //          0x01,       // Unit Identifier
            //          0x03,       // Function Code
            //          0x07, 0xD2, // Starting Address (2002 轉成 2 bytes 的結果)
            //          0x00, 0x01, // Quantity of Registers (1 轉成 2 bytes 的結果)
            //};
            short address = short.Parse(txtRegAddress.Text);
            short quantity = short.Parse(txtQuantity.Text);
            var startingAddress = BitConverter.GetBytes(address);
            var quantityOfRegisters = BitConverter.GetBytes(quantity);
            // TCP 傳送都是以 Big Endian
            if (BitConverter.IsLittleEndian)
            {
                startingAddress = startingAddress.Reverse().ToArray();
                quantityOfRegisters = quantityOfRegisters.Reverse().ToArray();
            }
            var request = new byte[]
            {
              0x00, 0x01, // Transaction Identifier、連續數字或隨機數字都可以
              0x00, 0x00, // Protocol Identifier、固定 0
              0x00, 0x06, // Length、接下來有 6 bytes 的資料
              0x01,       // Unit Identifier
              0x03,       // Function Code
              startingAddress[0], startingAddress[1],
              quantityOfRegisters[0], quantityOfRegisters[1] // Quantity of Registers (1 轉成 2 bytes 的結果)
            };

            // 建立 TcpClient 並進行連線
            var tcpClient = new TcpClient();
            tcpClient.Connect(txtIPAddress.Text, int.Parse(txtPort.Text));

            NetworkStream stream = tcpClient.GetStream();
            stream.Write(request, 0, request.Length);

            var response = new byte[1024];
            var readCount = stream.Read(response, 0, response.Length);
            Console.WriteLine("readCount = " + readCount.ToString());
            // 比較完整的寫法是把 Transaction Identifier、Function Code 等都抓出來檢查
            // 尤其是 Function Code，因為當有 error 時就不會是 0x03，而是 0x83 了
            // 不過這邊就先省略，以抓到我們的目標資料為主，那麼就是最後的 quantity * 2 個 bytes
            //var result = response.Take(readCount).TakeLast(quantity * 2).ToArray();
            var result = response.Skip(readCount - (quantity * 2)).Take(quantity * 2).ToArray();
            string hexString = BitConverter.ToString(result).Replace("-", "");
            Console.WriteLine("result = " + hexString);
            txtResultHex.Text = hexString;
            txtResultDec.Text = Convert.ToInt32(hexString, 16).ToString();
        }
    }
}
