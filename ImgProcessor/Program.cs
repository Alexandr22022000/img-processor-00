using System;
using System.Drawing;
using System.Threading;
using System.Collections;

namespace ImgProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            MyImg img = MyImg.OpenImg("D:\\Date\\test.png");    //открытие картинки 

            ArrayList fires = Fire.Get(img);                    //поиск пожаров

            Fire.Print(fires);                                  //вывод пожаров 

            Console.ReadLine();
        }   
    }

    /*
     * Класс для работы с изображениями 
     */
    class MyImg
    {
        public int width, height;
        public byte[,,] data;

        /*
         * Класс изображения (создал для удобства)
         */
        public MyImg(int width, int height, byte[,,] data)
        {
            this.data = data;   //трехмерный массив [x, y, pixel]
            this.width = width;
            this.height = height;
        }


        public static MyImg OpenImg(String url)
        {
            Bitmap original = new Bitmap(Image.FromFile(url));

            int width = original.Width,
                height = original.Height;
            byte[,,] res = new byte[width, height, 3];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Color color = original.GetPixel(x, y);
                    res[x, y, 0] = color.R;
                    res[x, y, 1] = color.G;
                    res[x, y, 2] = color.B;
                }
            }
            return new MyImg(width, height, res);
        }

        public void SaveImg(String url)
        {
            Bitmap bitmap = new Bitmap(width, height);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Color color = Color.FromArgb(data[x, y, 0], data[x, y, 1], data[x, y, 2]);
                    bitmap.SetPixel(x, y, color);
                }
            }

            bitmap.Save(url);
        }
    }


    class Fire
    {
        private const int FIRE_TEMPERATURE = 100;
        private const int FIRE_RANGE = 1;

        private static int cheeckedPixelsCount = 0;
        private static MyImg img;
        public static ArrayList fires;

        /*
         * Вывод информации о подарах в консоль
         */
        public static void Print (ArrayList fires)
        {
            foreach (FireZone fire in fires)
            {
                Console.WriteLine("----+++----");
                Console.WriteLine("X = " + fire.zone.GetCoordinates()[0] + "; Y = " + fire.zone.GetCoordinates()[1] + ";");
                Console.WriteLine("S = " + fire.pixels + " pixels"); 
                Console.WriteLine("----+++----");
            }
        }

        /*
         * ---Основной расчет площади---
         */
        public static ArrayList Get (MyImg imgg)
        {
            img = imgg;
            fires = new ArrayList();

            AddLog();   //добавляет информацию о скорости работы алгоритма (на сам алгоритм не влияет)

            for (int x = 0; x < img.width; x++)
            {
                for (int y = 0; y < img.height; y++)        //перебор всех пикселей
                {
                    cheeckedPixelsCount++;                  //отслеживание провереных пикселей (для отслеживания скорости, на алгоритм не влияет)
                    if (IsFire(x, y))
                    {
                        //проверяет не принадлежит ли пиксель к уже известному пажару 
                        bool isFirst = true;
                        foreach (FireZone fire in fires)
                        {
                            if (fire.zone.Included(x, y)) isFirst = false;
                        }

                        if (isFirst)
                        {
                            CalcZone(x, y); //если не пренадлижит начинает расчет группы
                        }

                    }
                }
            }

            return fires;
        }

        /*
         * Функция для отслеживания скорости работы алгоритма (не влияет на алгоритм)
         */
        private static void AddLog ()
        {
            (new System.Threading.Thread(delegate () {
                while (cheeckedPixelsCount < img.height * img.width)
                {
                    Console.WriteLine(cheeckedPixelsCount + "/" + img.height * img.width + " - " + (cheeckedPixelsCount * 100 / (img.height * img.width)) + "%");
                    Thread.Sleep(100);
                }
            })).Start();
        }

        /*
         * Расчет очага пожара 
         */
        private static void CalcZone (int x, int y)
        {
            Zone searchZone = new Zone(x - FIRE_RANGE, x + FIRE_RANGE, y - FIRE_RANGE, y + FIRE_RANGE), //зона поиска горящих пикселей
                fireZone = new Zone(x, x, y, y);                                                        //зона с горящими пикселями

            bool isDetected = false;

            while (!isDetected)     //работает пока обнаруживаются новые горящие пиксили 
            {
                isDetected = true;

                searchZone.ZoneLoop(delegate (int ix, int iy)           //переберает все пиксили в зоне поиска 
                {
                    if (IsFire(ix, iy) && !fireZone.Included(ix, iy))   //проверяет не горит лт пиксель и не входит ли в уже известную зону
                    {
                        fireZone.Expand(ix, iy);                        //увеличивает горящую зону до обнаруженного пиксиля
                        isDetected = false;
                    }
                });

                searchZone = new Zone(fireZone.minX - FIRE_RANGE, fireZone.maxX + FIRE_RANGE, fireZone.minY - FIRE_RANGE, fireZone.maxY + FIRE_RANGE);  //создает новую зону поиска на основе зоны пожара + дистанция между очагами
            }

            int count = 0;

            fireZone.ZoneLoop(delegate (int ix, int iy)     //переберает все пиксили в горящей зоне
            {
                if (IsFire(ix, iy))     //если горящий пиксель, то увеличить площадь
                {
                    count++;       
                }
            });

            fires.Add(new FireZone(fireZone, count));   //запомнить очаг 
        } 

        /*
         * Проверяет горит ли пиксиль
         */
        private static bool IsFire (int x, int y)
        {
            return (img.data[x, y, 0] + img.data[x, y, 1] + img.data[x, y, 2]) >= FIRE_TEMPERATURE * 3;
        }

        /*
         * Класс для хранения данных о очагах
         */
        class FireZone
        {
            public int pixels;
            public Zone zone;

            public FireZone (Zone zone, int pixels)
            {
                this.zone = zone;
                this.pixels = pixels;
            }
        }

        /*
         * класс для работы с зоной 2D пространства
         */
        class Zone
        {
            public delegate void LoopFunc(int x, int y); //делегат для перебора пикселей 

            public int minX, maxX, minY, maxY;          //раници зоны

            /*
             * конструктор для зоны
             */
            public Zone(int minX, int maxX, int minY, int maxY)
            {
                if (minX < 0) minX = 0;
                if (minY < 0) minY = 0;
                if (maxX >= img.width) maxX = img.width - 1;
                if (maxY >= img.height) maxY = img.width - 1;

                this.minX = minX;
                this.maxX = maxX;
                this.minY = minY;
                this.maxY = maxY;
            }

            /*
             * перебор пикселей в зоне 
             */
            public void ZoneLoop (LoopFunc loopFunc)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        loopFunc(x, y);
                    }
                }
            }

            /*
             * содержит ли зона пиксель
             */
            public bool Included (int x, int y)
            {
                return minX <= x && x <= maxX && minY <= y && y <= maxY;
            }

            /*
             * содержит ли зона пиксель
             */
            public void Expand (int x, int y)
            {
                if (x > maxX) maxX = x;
                if (x < minX) minX = x;
                if (y > maxY) maxY = y;
                if (y < minY) minY = y;
            }


            /*
             * вычислить средние координаты зоны
             */
            public int[] GetCoordinates ()
            {
                int x = (maxX + minX) / 2,
                    y = (maxY + minY) / 2;

                return new int[] { x, y };
            }
        }
    }
}
