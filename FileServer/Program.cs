using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace HW_FileTransferServer
{
    internal class Program
    {
        private static ICollection<TcpClient> clients = new List<TcpClient>();

        static async Task Main(string[] args)
        {
            IEnumerable<IPAddress> addresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in addresses)
                Console.WriteLine(address);

            

            TcpListener listener = new TcpListener(
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2024));
            listener.Start();

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                lock (clients)
                    clients.Add(client);
                ServeClient(client);
            }
        }

        private static async void ServeClient(TcpClient client)
        {
            while (true)
            {
                // клиент посылает нам файл
                //   длина имени файла
                long fileNameLength = await ReceiveInt64(client);
                await SendAllInt64(fileNameLength);
                //   имя файла
                byte[] fileNameBytes = await ReceiveFixed(client, (int)fileNameLength);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);
                await SendAllBytes(fileNameBytes);

                //   длина содержимого файла
                long fileContentLength = await ReceiveInt64(client);
                await SendAllInt64(fileContentLength);
                //   содержимое файла
                await ReceiveFileContent(client, fileName, fileContentLength);
            }
        }

        private static async Task<byte[]> ReceiveFixed(TcpClient client, int length)
        {
            byte[] bytes = new byte[length];
            int pos = 0;
            while (pos < length)
            {  // ReadExactlyAsync
                int read = await client.GetStream().ReadAsync(bytes, pos, length - pos);
                pos += read;
            }
            return bytes;
        }
        private static async Task<long> ReceiveInt64(TcpClient client)
        {
            byte[] bytes = await ReceiveFixed(client, sizeof(long));
            return BitConverter.ToInt64(bytes);
        }
        private static async Task ReceiveFileContent(TcpClient client, string filename, long length)
        {
            using Stream file = File.Create(filename);
            long pos = 0;
            byte[] bytes = new byte[1024];
            while (pos < length)
            {
                long read = await client.GetStream().ReadAsync(bytes, 0,
                    Math.Min(bytes.Length, (int)(length - pos)));
                // await file.WriteAsync (bytes, 0, (int) read);
                await SendAllBytes(bytes, 0, (int)read);
                pos += read;
            }
        }

        private static async Task SendAllBytes(byte[] bytes)
        {
            foreach (TcpClient client in clients)
                await client.GetStream().WriteAsync(bytes);
        }
        private static async Task SendAllBytes(byte[] bytes, int pos, int length)
        {
            foreach (TcpClient client in clients)
                await client.GetStream().WriteAsync(bytes, pos, length);
        }
        private static async Task SendAllInt64(long value)
        {
            await SendAllBytes(BitConverter.GetBytes(value));
        }
    }
}

