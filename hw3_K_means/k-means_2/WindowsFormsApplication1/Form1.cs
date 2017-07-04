using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Drawing.Imaging; // for ImageFormat
using System.IO;//輸入讀取
using System.Globalization;
using System.Diagnostics;

namespace WindowsApplication1
{
    public partial class Form1 : Form
    {
        int[,,] RGBdata;//壓縮後的陣列
        int[,,] COMdata;//壓縮後的陣列
        String Filename;//檔案名稱
        String new_filename = @"C:\Temp\Output.txt";

        public Form1()
        {
            InitializeComponent();
        }
        // Load 按鈕事件處理函式 
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.Cancel)
            {
                Filename = openFileDialog1.FileName;                
                ImageForm MyImage = new ImageForm(openFileDialog1.FileName); // 建立秀圖物件
                MyImage.Show();// 顯示秀圖照片 
            }

        }
        // 壓縮 按鈕事件處理函式 
        private void button2_Click(object sender, EventArgs e)
        {
            Compression Compression = new Compression(openFileDialog1.FileName, out RGBdata, this.inputText.Text);// 建立秀圖物件
            Compression.Show();// 顯示秀圖照片
        }
        //show 壓縮率&失真率
        private void button3_Click(object sender, EventArgs e)
        {
            int result;//失真率
            //計算失真率
            int temp = 0, count = 0;
            for (int i = 0; i < COMdata.GetUpperBound(0); i++)
            {
                for (int j = 0; j < COMdata.GetUpperBound(1); j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        temp += Math.Abs(COMdata[i, j, k] - RGBdata[i, j, k]);
                        count++;
                    }

                }
            }
            result = temp / count;

            //計算壓縮率
            String x = Filename;            
            Double s1 = new FileInfo(x).Length;            
            Double s2 = new FileInfo(new_filename).Length;
            int compress = (int)(s2 / s1 * 100);            
            MessageBox.Show("失真率：" + result + "%" + "\n壓縮率：" + compress + "%");
        }
        // 建立一個專門秀圖的 Form 類別
        class ImageForm : Form
        {
            Image image; // 建構子 
            public ImageForm(String Filename)
            {
                LoadImage(Filename);
                InitializeMyScrollBar();

            }

            public void LoadImage(String Filename)
            {   //載入檔案
                image = Image.FromFile(Filename);
                this.Text = Filename;
                //調整視窗大小
                this.Height = image.Height;
                this.Width = image.Width;
            }
            //ScrollBar視窗滾動
            private void InitializeMyScrollBar()
            {
                VScrollBar vScrollBar1 = new VScrollBar();
                HScrollBar hScrollBar1 = new HScrollBar();
                vScrollBar1.Dock = DockStyle.Right;
                hScrollBar1.Dock = DockStyle.Bottom;
                Controls.Add(vScrollBar1);
                Controls.Add(hScrollBar1);
                
            }
            //顯示圖片
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.DrawImage(image, 0, 0, image.Width, image.Height);
            }
        }
        class Compression : Form
        {
            Image image; // 建構子
            int clu;
            int Height, Width;//圖片的寬跟高
            Bitmap bimage;
            String Filename;//讀入的檔案名稱
            String new_filename = @"C:\Temp\Output.txt";

            public Compression(String Filename, out int[,,] RGB, String in_clu)
            {
                clu = Int32.Parse(in_clu);
                double[,] centerarr = new double[clu, 3];
                double[,] preclass = new double[clu, 3];
                int count = 0;
                
                LoadImage(Filename);

                RGB = new int[Height, Width, 3];
                RGB = getRGBData();

                int[,] classnum = new int[Height, Width];
                initpoint(classnum,RGB);
                newcenter(centerarr, classnum, RGB);
                int times = 0;
                while (count != clu*3)
                {
                    count = 0;
                    clustal(RGB, centerarr, classnum);
                    Debug.WriteLine("第{0}次分群。",++times);
                    for (int i = 0; i < clu; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            preclass[i, j] = centerarr[i, j];
                        }
                    }
                    newcenter(centerarr, classnum, RGB);
                    for (int i = 0; i < clu; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if(preclass[i, j] == centerarr[i, j])
                            {
                                count++;
                            }
                        }
                    }
                }
                resetRGB(RGB, centerarr, classnum);
                for (int i = 0; i < clu; i++)
                {
                    for(int j = 0; j < 3; j++)
                    {
                        Debug.Write(centerarr[i,j].ToString()," ");
                    }
                    Debug.WriteLine("");
                }
            }

            public void LoadImage(String Filename)
            {   //載入檔案
                image = Image.FromFile(Filename);
                this.Text = Filename;
                //調整視窗大小
                this.Height = image.Height;
                this.Width = image.Width;
            }
            public int[,,] getRGBData()
            {
                // Step 1: 利用 Bitmap 將 image 包起來
                bimage = new Bitmap(image);
                Height = bimage.Height;
                Width = bimage.Width;
                //初始化陣列
                int[,,] rgbData = new int[Height, Width, 3];

                // Step 2: 取得像點顏色資訊
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Color color = bimage.GetPixel(x, y);
                        rgbData[y, x, 0] = color.R;
                        rgbData[y, x, 1] = color.G;
                        rgbData[y, x, 2] = color.B;
                    }
                }
                return rgbData;
            }
            /*K-means隨機找起始值*/
            public void initpoint(int[,] classnum, int[,,] RGB)
            {
                Random rnd = new Random();

                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        //int rannum = (rnd.Next(0, Width * Height + 100)) % 3;
                        classnum[i,j] = rnd.Next(0,clu);
                    }
                }
            }
            /*計算各點距離去分類*/
            public void clustal(int[,,] rgbData, double[,] centerarr, int[,] classnum)
            {
                for(int i = 0; i < Height; i++)
                {
                    for(int j = 0; j < Width; j++)
                    {
                        double min = 999999999; 
                        for(int k = 0; k < clu; k++)
                        {
                            double dis = dist(rgbData[i, j, 0], rgbData[i, j, 1], rgbData[i, j, 2], centerarr[k, 0], centerarr[k, 1], centerarr[k, 2]);
                            if (dis < min)
                            {
                                classnum[i, j] = k;
                                min = dis;
                            }
                        }
                    }
                }
            }
            
            /*計算兩點距離*/
            public double dist(int x1,int y1,int z1, double x2, double y2, double z2)
            {
                double x = Math.Pow((x1 - x2), 2);
                double y = Math.Pow(y1 - y2, 2);
                double z = Math.Pow(z1 - z2, 2);
                double dis = x +y  +z;
                return dis;
            }
            /*計算新的centernum*/
            public void newcenter(double[,] centerarr, int[,] classnum, int[,,] rgbData)
            {
                int[,] total = new int[clu,3];
                int[] count = new int[clu];
                for (int i = 0; i < Height; i++)
                {
                    for(int j = 0; j < Width; j++)
                    {
                        int cl_num = classnum[i, j];
                        count[cl_num]++;
                        total[cl_num, 0] += rgbData[i, j, 0];
                        total[cl_num, 1] += rgbData[i, j, 1];
                        total[cl_num, 2] += rgbData[i, j, 2];
                    }
                }
                for(int i = 0; i < clu; i++)
                {
                    if(count[i] == 0)
                    {
                        count[i] = 1;
                    }
                }
                for(int k = 0; k < clu; k++)
                {
                    centerarr[k, 0] = total[k, 0] / count[k];
                    centerarr[k, 1] = total[k, 1] / count[k];
                    centerarr[k, 2] = total[k, 2] / count[k];
                }
            }
            
            /*重新上色*/
            public void resetRGB(int[,,] rgbData, double[,] centerarr, int[,] classnum)
            {
                for(int i = 0; i < Height; i++)
                {
                    for(int j = 0; j < Width; j++)
                    {
                        rgbData[i, j, 0] = (int)centerarr[classnum[i, j], 0];
                        rgbData[i, j, 1] = (int)centerarr[classnum[i, j], 1];
                        rgbData[i, j, 2] = (int)centerarr[classnum[i, j], 2];
                    }
                }
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        bimage.SetPixel(x, y, Color.FromArgb(rgbData[y, x, 0], rgbData[y, x, 1], rgbData[y, x, 2]));
                    }
                }
                image = bimage;
                
                this.Refresh();
            }
          
            public void doGray(int[,,] rgbData)
            {
                // Step 1: 建立 Bitmap 元件
                Bitmap bimage = new Bitmap(image);
                int Height = bimage.Height;
                int Width = bimage.Width;
                // Step 2: 設定像點資料
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        int gray = (rgbData[x, y, 0] + rgbData[x, y, 1] + rgbData[x, y, 2]) / 3;
                        bimage.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                    }
                }
                // Step 3: 更新顯示影像 
                image = bimage;
                this.Refresh();
            }
            //ScrollBar視窗滾動
            private void InitializeMyScrollBar()
            {
                VScrollBar vScrollBar1 = new VScrollBar();
                HScrollBar hScrollBar1 = new HScrollBar();
                vScrollBar1.Dock = DockStyle.Right;
                hScrollBar1.Dock = DockStyle.Bottom;
                Controls.Add(vScrollBar1);
                Controls.Add(hScrollBar1);
            }
            //顯示圖片
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.DrawImage(image, 0, 0, image.Width, image.Height);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}