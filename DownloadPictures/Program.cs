using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Net;

namespace DownloadPictures
{
    internal class Program
    {
        static CancellationTokenSource cts;
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelHandler);
            DownloadSettings? downloadSettings = GetSettings<DownloadSettings>("DownloadSettings");
            WriteLikeTypeWriter("Merhaba", 50);
            WriteLikeTypeWriter("Resimleriniz aşağıda belirtilen ayarlarla indirilmeye başlayacaktır.");
            WriteLikeTypeWriter($"\t {"İndirilecek Toplam Dosya Sayısı",-40}:{downloadSettings.Count}");
            WriteLikeTypeWriter($"\t {"Parallel Download Limit",-40}:{downloadSettings.Parallelism}");
            WriteLikeTypeWriter($"\t {"İndirme Yapılacak Adres",-40}:{downloadSettings.Url}");
            WriteLikeTypeWriter($"\t {"Resimlerin Kaydedileceği Adres",-40}:{downloadSettings.SavePath}");
            WriteLikeTypeWriter($"İndirme işlemini başlatmak için lütfen bir tuşa basın...");
            Console.ReadKey();
            Console.WriteLine();
            WriteLikeTypeWriter($"İndirme işleminiz başlıyor...");
            DownloadImages(downloadSettings);


            Console.ReadLine();
        }
        protected static void CancelHandler(object sender, ConsoleCancelEventArgs args)
        {
            CancelOperations();
            Thread.Sleep(3000);
            args.Cancel = true;
        }

        private static void CancelOperations()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("İşleminiz iptal ediliyor.");
            cts.Cancel();
            ClearFiles();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("İptal işlemi tamamlandı. Proğramı kapatmak için lütfen tekrar ctrl+c kombinasyonuna basınız.");
        }

        private static void ClearFiles() => Directory.Delete(GetSettings<DownloadSettings>("DownloadSettings").SavePath, true);


        /// <summary>
        /// Resimleri indiren methoddur.Parallel olarak çalışır. Thread.Sleep ifadesi proggress ilerlemesini daha net 
        /// görmek amacıyla eklenmiştir.
        /// </summary>
        /// <param name="settings"></param>
        private static void DownloadImages(DownloadSettings settings)
        {
            CheckCreateDirectory(settings);
            Random rnd = new Random();
            try
            {
                Parallel.For(0, settings.Count, GetParallelOptions(settings), x =>
                {

                    var webClient = new WebClient();
                    webClient.DownloadFileCompleted += DownloadCompleted;
                    webClient.DownloadFileTaskAsync(new Uri(settings.Url), Path.Combine(settings.SavePath, x + ".jpg"));

                    Thread.Sleep(rnd.Next(0, 1000));
                });
                Console.WriteLine();
                Console.WriteLine("Tebrikler indirme işlemi başarıyla tamamlandı.");
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                cts.Dispose();
            }
        }

        private static ParallelOptions GetParallelOptions(DownloadSettings settings)
        {
            cts = new CancellationTokenSource();
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = settings.Parallelism, CancellationToken = cts.Token };
            return parallelOptions;
        }

        private static void CheckCreateDirectory(DownloadSettings settings)
        {
            if (!Directory.Exists(settings.SavePath))
            {
                Directory.CreateDirectory(settings.SavePath);
            }
        }

        static int completedCount = 1;
        private static void DownloadCompleted(object? sender, AsyncCompletedEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"\r{completedCount} / {GetSettings<DownloadSettings>("DownloadSettings").Count} -- Progress");
            completedCount++;
            Console.ForegroundColor = ConsoleColor.White;

        }

        private static T GetSettings<T>(string sectionName)
        {
            IConfiguration config = SetupConfiguration();

            return config.GetSection(sectionName).Get<T>();
        }

        private static IConfiguration SetupConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("Input.json", optional: true, reloadOnChange: true);

            IConfiguration config = builder.Build();
            return config;
        }

        public static void WriteLikeTypeWriter(string text, int? speed = 15)
        {
            foreach (var c in text)
            {
                Console.Write(c);
                Thread.Sleep(speed.Value);
            }
            Console.WriteLine();
        }
    }
}