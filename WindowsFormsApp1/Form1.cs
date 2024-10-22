using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Schema;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        Stream myStream;
        Mutex[,] mutList;
        MyThread[] threadLst;
        const int xCell = 30;
        const int yCell = 30;
        const int xMax = 25;
        const int yMax = 20;
        
        public Random rnd = new Random();

        void MySleep(long count)
        {
            long i, j, k = 0;
            for (i = 0; i < count; i++)
                for (j = 0; j < 6400; j++) k = k + 1;
        }

        void Running(object obj)
        {
            MyThread p = (MyThread)obj;
            Graphics g = this.CreateGraphics();
            Brush brush = new SolidBrush(Color.FromArgb(0, 0, 0));
            mutList[p.Pos.Y, p.Pos.X].WaitOne();
            int x1, y1;
            int x2, y2;
            int x, y;
            bool kq = true;
            try
            {
                while (p.start)
                { //lặp trong khi chưa có yêu cầu kết thúc
                  //xác ₫ịnh tọa ₫ộ hiện hành của thread
                    x1 = p.Pos.X; y1 = p.Pos.Y;
                    //hiển thị logo của thread ở (x1,y1)
                    g.DrawImage(p.Pic, xCell * x1, yCell * y1);
                    Color c = p.Pic.GetPixel(1, 1);
                    int yR, yG, yB;
                    if (c.R > 128) yR = 0; else yR = 255;
                    if (c.G > 128) yG = 0; else yG = 255;
                    if (c.B > 128) yB = 0; else yB = 255;
                    Pen pen = new Pen(Color.FromArgb(yR, yG, yB), 2);
                    if (p.tx >= 0 && p.ty >= 0)
                    { //hiện mũi tên góc dưới phải
                        x = xCell * x1 + xCell - 2;
                        y = yCell * y1 + yCell - 2;
                        g.DrawLine(pen, x, y, x - 10, y);
                        g.DrawLine(pen, x, y, x, y - 10);
                    }
                    else if (p.tx >= 0 && p.ty < 0)
                    { //hiện mũi tên góc trên phải
                        x = xCell * x1 + xCell - 2;
                        y = yCell * y1 + 2;
                        g.DrawLine(pen, x, y, x - 10, y);
                        g.DrawLine(pen, x, y, x, y + 10);
                    }
                    else if (p.tx < 0 && p.ty >= 0)
                    { //hiện mũi tên góc dưới trái
                        x = xCell * x1 + 2;
                        y = yCell * y1 + yCell - 2;
                        g.DrawLine(pen, x, y, x + 10, y);
                        g.DrawLine(pen, x, y, x, y - 10);
                        
                    }
                    else
                    {//hiện mũi tên góc trên trái
                        x = xCell * x1 + 2;
                        y = yCell * y1 + 2;
                        g.DrawLine(pen, x, y, x + 10, y);
                        g.DrawLine(pen, x, y, x, y + 10);
                    }
                    //giả lập thực hiện công việc của thread tốn 500ms
                    MySleep(500);
                    //xác ₫ịnh vị trí mới của thread
                    p.HieuchinhVitri();
                    x2 = p.Pos.X; y2 = p.Pos.Y;
                    //xin khóa truy xuất cell (x2,y2) 
                    mutList[y2, x2].WaitOne();
                    // Xóa vị trí cũ
                    g.FillRectangle(brush, xCell * x1, yCell * y1, xCell, yCell);
                    //trả cell (x1,y1) cho các thread khác truy xuất 
                    mutList[y1, x1].ReleaseMutex();
                }
            }
            catch (Exception e) { p.t.Abort(); }
            //dọn dẹp thread trước khi ngừng
            x1 = p.Pos.X; y1 = p.Pos.Y;
            g.FillRectangle(brush, xCell * x1, yCell * y1, xCell, yCell);
            //trả cell (x1,y1) cho các thread khác truy xuất 
            mutList[y1, x1].ReleaseMutex();
            // dừng Thread
            p.stop = true;
            p.t.Abort();
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Reflection.Assembly myAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            threadLst = new MyThread[26];
            int i;
            mutList = new Mutex[yMax, xMax];
            int h, cot;
            for (h = 0; h < yMax; h++)
                for (cot = 0; cot < xMax; cot++)
                    mutList[h, cot] = new Mutex();
            for (i = 0; i < 26; i++)
            {
                threadLst[i] = new MyThread(rnd, xMax, yMax);
                threadLst[i].stop = threadLst[i].suspend = threadLst[i].start = false;
                char c = (char)(i + 65);
                myStream = myAssembly.GetManifestResourceStream("ThreadDemo3.Resources.image" + c.ToString() + ".bmp");
                threadLst[i].Pic = new Bitmap(myStream);
                threadLst[i].Xmax = 25;
                threadLst[i].Ymax = 20;
            }
            ClientSize = new Size(25 * 30, 20 * 30);
            this.Location = new Point(0, 0);
            this.BackColor = Color.Blue;   
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            int newch = e.KeyValue;
            if (newch < 0x41 || newch > 0x5a)return;
            if (e.Control && e.Shift)
            {
                threadLst[newch - 65].start = false;
            }
            else if (e.Alt)
            {
                if (threadLst[newch - 65].start && threadLst[newch-65].suspend)
                {
                    threadLst[newch - 65].t.Resume();
                    threadLst[newch - 65].suspend = false;
                }    
            } 
            else if (e.Shift)
            {
                threadLst[newch - 65].t.Priority = ThreadPriority.Highest;
                MessageBox.Show(threadLst[newch - 65].t.Priority.ToString());
            }    
            else if(e.Control)
            {
                threadLst[newch - 65].t.Priority = ThreadPriority.Lowest;
                MessageBox.Show(threadLst[newch - 65].t.Priority.ToString());
            }
            else
            {
                if (!threadLst[newch - 65].start)
                {
                    threadLst[newch - 65].start = true;
                    threadLst[newch - 65].suspend = false;
                    threadLst[newch - 65].t = new Thread(new ParameterizedThreadStart(Running));
                    if (newch == 65) threadLst[newch - 65].t.Priority = ThreadPriority.Highest;
                    else threadLst[newch - 65].t.Priority = ThreadPriority.Lowest;
                    threadLst[newch - 65].t.Start(threadLst[newch - 65]);
                }    
            }    
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            int i;
            for (i = 0; i < 26; i++)
                if (threadLst[i].start)
                {
                    threadLst[i].start = false;
                    while (!threadLst[i].stop) ;
                }    
        }
    }
}
