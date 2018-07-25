using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Miner
{
    public partial class Form1 : Form
    {
        public static int M = 9, N = 9, quanOfMines = 10;
        public static int len = 30, offset = 80, widthOfField, heightOfField, widthOfWindow, heightOfWindow; // учитываем края
        public static int face_len = 40, face_x, face_y;   // координаты и размер поля для морды
        public static bool face_click = false;    // нажали ли на морду?

        public static Random rnd = new Random();

        public int[,] digits;   // матрицы цифр (0 - нажали и тут пусто, далее 1,2,3... по правилам, 9 - мина)
        public int[,] flags;    // матрица флажков (0 - не нажали (не отображаем, не видно), 1 - нажали (отобразить, видно), 2 - тут флажок, 3 - тут взрыв, 4 - тут вопрос)

        public Queue<Point> q = new Queue<Point>();

        public bool first_click = true, endOfGame = false;
        public int viewQuanOfMines = quanOfMines;
        public int timer = 0;
        public int face = 0;  // 0 - обычное, 1 - нажали, 2 - проигрыш, 3 - очки


        public Form1()
        {
            InitializeComponent();
            newGame();
        }

        public void newGame(){
            widthOfField = len * M;
            heightOfField = len * N;
            widthOfWindow = widthOfField + 17;
            heightOfWindow = heightOfField + offset + 40;

            // настраиваем размеры окна
            this.Width = widthOfWindow;
            this.Height = heightOfWindow;

            // формируем матрицы
            digits = new int[M, N];
            flags = new int[M, N];
            generateDigits();
            zeroFlags();

            // рисуем окошко со временем
            timer = 0;
            label2.Left = this.Width - label2.Width - 30;
            label2.Text = timer.ToString("D3");

            // рисуем кол-во флажков
            viewQuanOfMines = quanOfMines;

            // рисуем окно для морды
            face_x = (label1.Right + label2.Left - face_len) / 2;
            face_y = 33;
            face = 0;

            first_click = true;
            endOfGame = false;
            timer1.Stop();

            Invalidate();
        }

        public void generateDigits()
        {
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    digits[i, j] = 0;
                }
            }

            // ставим мины
            for (int i = 0; i < quanOfMines; i++)
            {
                int x = 0, y = 0;

                do
                {
                    x = rnd.Next(0, M);
                    y = rnd.Next(0, N);
                } while (digits[x, y] == 9);

                digits[x, y] = 9;
            }

            // ставим цифры
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    // осмотр соседей и подсчет среди них мин (9)
                    if (digits[i, j] != 9)
                    {
                        for (int k = -1; k <= 1; k++)
                        {
                            for (int m = -1; m <= 1; m++)
                            {
                                if ((k != 0 || m != 0) && i + k >= 0 && i + k < M && j + m >= 0 && j + m < N && digits[i + k, j + m] == 9) // за границы не заходим
                                {
                                    digits[i, j]++;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void zeroFlags()
        {
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    flags[i, j] = 0;
                }
            }
        }

        public void openNeighbours(int i, int j)
        {
            q.Clear();
            q.Enqueue(new Point(i, j));

            while (q.Count > 0)
            {
                Point p = q.Dequeue();
                int i_ = p.X;
                int j_ = p.Y;
                flags[i_, j_] = 1;

                if (digits[i_, j_] == 0)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        for (int m = -1; m <= 1; m++)
                        {
                            if (i_ + k >= 0 && i_ + k < M && j_ + m >= 0 && j_ + m < N && digits[i_ + k, j_ + m] != 9)  // за границы не заходим. Только непосредственные соседи
                            {
                                if (flags[i_ + k, j_ + m] == 0)  // помещаем в очередь всех соседей нулей
                                {
                                    q.Enqueue(new Point(i_ + k, j_ + m));
                                }
                            }
                        }
                    }
                }
            }
        }

        public void toggleFlag(int i, int j)
        {
            if (flags[i, j] != 1)
            {
                viewQuanOfMines += (flags[i, j] == 0) ? -1 : (flags[i, j] == 2) ? 1 : 0;
                flags[i, j] = (flags[i, j] + 2) % 6;
            }
        }

        public void explosion(int i, int j)     // выполняются операции по взрыву
        {
            // взорвать всё, а это поле вообще красным фоном отметить
            for (int k = 0; k < M; k++)
            {
                for (int m = 0; m < N; m++)
                {
                    if (digits[k, m] == 9)
                    {
                        flags[k, m] = 1;
                    }
                }
            }

            flags[i, j] = 3;

            // сменить морду
            face = 2;

            // запретить ходы
            endOfGame = true;
            timer1.Stop();
        }

        public void checkWin()
        {
            bool allOpen = true;   // все ли клетки открыты?

            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    if (flags[i, j] == 0)
                    {
                        allOpen = false;
                    }
                }
            }

            endOfGame = endOfGame || allOpen && viewQuanOfMines == 0;
            if (endOfGame)
            {
                if (face != 2)   // если человечек не загрустил, то одевает очки
                {
                    face = 3;
                }
                timer1.Stop();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Pen dark_pen = new Pen(Color.Gray, 3);
            Pen dark_thin_pen = new Pen(Color.Gray);
            Pen white_pen = new Pen(Color.White, 3);
            Pen black_pen = new Pen(Color.Black, 3);
            Pen black_thin_pen = new Pen(Color.Black);

            Font font = new Font("Courier", 20, FontStyle.Bold);

            Brush back_brush = new SolidBrush(Color.LightGray);
            Brush font_brush = new SolidBrush(Color.Green);
            Brush black_brush = new SolidBrush(Color.Black);
            Brush white_brush = new SolidBrush(Color.White);
            Brush red_brush = new SolidBrush(Color.Red);
            Brush yellow_brush = new SolidBrush(Color.Yellow);

            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    g.FillRectangle(back_brush, i * len, offset + j * len, len - 1, len - 1);  // квадратики
                    
                    if (flags[i, j] == 0)   // это то что скрыто
                    {
                        // темно-серые линии
                        //g.DrawLine(dark_pen, i * len - 1, offset + (j + 1) * len - 1, (i + 1) * len - 1, offset + (j + 1) * len - 1);  // горизонтальная
                        //g.DrawLine(dark_pen, (i + 1) * len - 1, offset + j * len + 2, (i + 1) * len - 1, offset + (j + 1) * len - 1);  // вертикальная

                        //// белые линии
                        //g.DrawLine(white_pen, i * len, offset + j * len, (i + 1) * len, offset + j * len);   // горизонтальная
                        //g.DrawLine(white_pen, i * len, offset + j * len, i * len, offset + (j + 1) * len);   // вертикальная

                        // темно-серые линии
                        g.DrawLine(dark_pen, i * len, offset + (j + 1) * len - 2, (i + 1) * len - 1, offset + (j + 1) * len - 2);     // горизонтальная
                        g.DrawLine(dark_pen, (i + 1) * len - 2, offset + j * len + 2, (i + 1) * len - 2, offset + (j + 1) * len - 1); // вертикальная

                        // белые линии
                        g.DrawLine(white_pen, i * len, offset + j * len + 1, (i + 1) * len, offset + j * len + 1);   // горизонтальная
                        g.DrawLine(white_pen, i * len + 1, offset + j * len, i * len + 1, offset + (j + 1) * len);   // вертикальная
                    }

                    if (flags[i, j] == 3)   // красный фон и конец
                    {
                       
                        g.FillRectangle(new SolidBrush(Color.Red), i * len, offset + j * len, len, len);
                    }

                    if (flags[i, j] == 1 || flags[i,j] == 3)  // открываем цифру
                    {
                        g.DrawRectangle(dark_thin_pen, i * len, offset + j * len, len, len);
                        switch (digits[i, j])
                        {
                            case 1: font_brush = new SolidBrush(Color.Blue); break;
                            case 2: font_brush = new SolidBrush(Color.Green); break;
                            case 3: font_brush = new SolidBrush(Color.Red); break;
                            case 4: font_brush = new SolidBrush(Color.DarkBlue); break;
                            case 5: font_brush = new SolidBrush(Color.Brown); break;
                            case 6: font_brush = new SolidBrush(Color.DarkCyan); break;
                            case 7: font_brush = new SolidBrush(Color.Black); break;
                            case 8: font_brush = new SolidBrush(Color.Gray); break;
                        }

                        if (digits[i, j] == 9)  // если вляпались в мину
                        {
                            //font_brush = new SolidBrush(Color.Black);
                            //g.DrawString("*", font, font_brush, i * len + 3, offset + j * len + 1);

                            g.FillEllipse(black_brush, i * len + len / 4, offset + j * len + len / 4, len / 2 + 1, len / 2 + 1);
                            // вертикаль
                            g.DrawLine(black_pen, i * len + len / 2, offset + j * len + len / 4 - len / 8, i * len + len / 2, offset + j * len + len / 4 + len / 2 + len / 8 + 1);
                            // горизонталь
                            g.DrawLine(black_pen, i * len + len / 4 - len / 8, offset + j * len + len / 2, i * len + len / 4 + len / 2 + len / 8 + 1, offset + j * len + len / 2);
                            // диагонали
                            g.DrawLine(black_pen, i * len + len / 4, offset + j * len + len / 4, i * len + len / 4 + len / 2 + 1, offset + j * len + len / 4 + len / 2 + 1);
                            g.DrawLine(black_pen, i * len + len / 2 + len / 4, offset + j * len + len / 4, i * len + len / 4 + 1, offset + j * len + len / 4 + len / 2 + 1);

                            // блик
                            g.FillEllipse(white_brush, i * len + len / 2 - len / 7, offset + j * len + len / 2 - len / 7, len / 7, len / 7);
                        }
                        else  if (flags[i, j] == 1 && digits[i, j] != 0)  // пишем цифру
                        {
                            g.DrawString(digits[i, j].ToString(), font, font_brush, i * len + 3, offset + j * len + 1);
                        }
                    }

                    if (flags[i, j] == 2)  // рисуем флажочек
                    {
                        g.FillRectangle(black_brush, i * len + len / 8 + 1 , offset + j * len + len * 3 / 4, len * 3 / 4, len / 8);
                        g.FillRectangle(black_brush, i * len + len / 4 + 1, offset + j * len + len * 3 / 4 - len / 8, len / 2, len / 8);
                        g.DrawLine(black_pen, i * len + len / 2, offset + j * len + len / 4 - len / 8, i * len + len / 2, offset + j * len + len / 4 + len / 2);
                        Point[] points = new Point[3];
                        points[0].X = i * len + len / 2 + 2;
                        points[0].Y = offset + j * len + len / 4 - len / 8 - 2;
                        points[1].X = points[0].X;
                        points[1].Y = offset + j * len + len / 4 + (len / 2 - len / 8) / 2 - 2;
                        points[2].X = i * len + len / 6;
                        points[2].Y = (points[0].Y + points[1].Y) / 2;
                        g.FillPolygon(red_brush, points);
                    }

                    if (flags[i, j] == 4)  // рисуем вопрос
                    {
                        g.DrawString("?", font, black_brush, i * len + 2, offset + j * len);

                    }

                }

            }

            // а теперь спец линии
            Pen spec_pen = new Pen(Color.DarkGray, 1);

            for (int i = 0; i <= M; i++)  // горизонтали
            {
                g.DrawLine(spec_pen, i * len, offset, i * len, offset + N * len);
            }

            for (int j = 0; j <= N; j++)   // вертикали
            {
                g.DrawLine(spec_pen, 0, offset + j * len, M * len, offset + j * len);
            }

            // морда
            int x = face_x;
            int y = face_y;

            if (!face_click)
            {
                g.FillRectangle(back_brush, x, y, face_len, face_len);  // квадратик
                // темно-серые линии
                g.DrawLine(dark_pen, x + face_len + 1, y + face_len, x, y + face_len);     // горизонтальная
                g.DrawLine(dark_pen, x + face_len, y + face_len + 1, x + face_len, y); // вертикальная
                // белые линии
                g.DrawLine(white_pen, x, y, x, y + face_len);   // горизонтальная
                g.DrawLine(white_pen, x, y, x + face_len, y);   // вертикальная
            }
            else
            {
                g.FillRectangle(back_brush, x, y, face_len - 1, face_len - 1);
                g.DrawRectangle(dark_thin_pen, x, y, face_len, face_len);
            }

            // закрашиваем лицо
            g.FillEllipse(yellow_brush, x + face_len / 8, y + face_len / 8, face_len * 3 / 4 + 1, face_len * 3 / 4 + 1);

            // рисуем контур лица
            g.DrawEllipse(black_thin_pen, x + face_len / 8, y + face_len / 8, face_len * 3 / 4 + 1, face_len * 3 / 4 + 1);
 
            if (face == 0)
            {
                // круглые глаза
                g.FillEllipse(black_brush, x + face_len / 3, y + face_len / 3, face_len / 8, face_len / 8);
                g.FillEllipse(black_brush, x + face_len * 2 / 3 - face_len / 12, y + face_len / 3, face_len / 8, face_len / 8);

                // улыбка
                g.DrawArc(black_thin_pen, x + face_len / 3, y + face_len / 2, face_len * 2/ 5, face_len / 4, 0, 180);
            }
            else if (face == 1)
            {
                // круглые глаза
                g.FillEllipse(black_brush, x + face_len / 3, y + face_len / 3, face_len / 8, face_len / 8);
                g.FillEllipse(black_brush, x + face_len * 2 / 3 - face_len / 12, y + face_len / 3, face_len / 8, face_len / 8);

                // круглый рот
                g.DrawEllipse(black_thin_pen, x + face_len * 2 / 5, y + face_len * 7 / 12, face_len / 5, face_len / 5);
            }
            else if (face == 2)
            {
                // крестики глаза
                g.DrawLine(black_thin_pen, x + face_len / 3, y + face_len / 3, x + face_len / 3 + face_len / 8, y + face_len / 3 + face_len / 8);
                g.DrawLine(black_thin_pen, x + face_len / 3 + face_len / 8, y + face_len / 3, x + face_len / 3, y + face_len / 3 + face_len / 8);

                g.DrawLine(black_thin_pen, x + face_len * 2 / 3 - face_len / 12, y + face_len / 3, x + face_len * 2 / 3 - face_len / 12 + face_len / 8, y + face_len / 3 + face_len / 8);
                g.DrawLine(black_thin_pen, x + face_len * 2 / 3 - face_len / 12, y + face_len / 3 + face_len / 8, x + face_len * 2 / 3 - face_len / 12 + face_len / 8, y + face_len / 3);

                // грусть
                g.DrawArc(black_thin_pen, x + face_len / 3, y + face_len * 7 / 12, face_len * 2 / 5, face_len / 4, 180, 180);
            }
            else if (face == 3)
            {
                // очки
                g.FillEllipse(black_brush, x + face_len / 3 - 1, y + face_len / 3, face_len / 5, face_len / 5);
                g.FillEllipse(black_brush, x + face_len * 2 / 3 - face_len / 12 - 1, y + face_len / 3, face_len / 5, face_len / 5);
                g.FillRectangle(black_brush, x + face_len / 3 - 1, y + face_len / 3, face_len / 5, face_len / 10);
                g.FillRectangle(black_brush, x + face_len * 2 / 3 - face_len / 12 - 1, y + face_len / 3, face_len / 5, face_len / 10);
                g.DrawLine(black_thin_pen, x + face_len / 3 - 1, y + face_len / 3, x + face_len * 2 / 3 - face_len / 12 - 1 + face_len / 5, y + face_len / 3);
                g.DrawLine(black_thin_pen,x + face_len / 3 - 1, y + face_len / 3, x + face_len / 8, y + face_len / 8 + face_len * 3 / 8);
                g.DrawLine(black_thin_pen, x + face_len * 2 / 3 - face_len / 12 - 1 + face_len / 5, y + face_len / 3, x + face_len / 8 + face_len * 3 / 4, y + face_len / 8 + face_len * 3 / 8);

                // улыбка
                g.DrawArc(black_thin_pen, x + face_len / 3, y + face_len / 2, face_len * 2 / 5, face_len / 4, 0, 180);
            }

            label1.Text = ((viewQuanOfMines + Math.Abs(viewQuanOfMines)) / 2).ToString("D3");  // выводим кол-во мин
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            int x = Cursor.Position.X - this.Left - 9;
            int y = Cursor.Position.Y - this.Top - 32;
            y -= offset;

            int i = x / len;
            int j = y / len;
            if (y < 0)   // проверяем чтобы тыкали прямо на поле
            {
                j = -1;
            }

            if (!endOfGame && i >= 0 && i < M && j >= 0 && j < N )
            {
                face = 1;
                timer1.Start();

                if (e.Button.ToString() == "Left")   // ЛКМ
                {
                    if (flags[i, j] != 2)  // если флажок, то нельзя ЛКМ
                    {
                        if (first_click)
                        {
                            first_click = false;
                            while (digits[i, j] != 0)  // на первом ходу ожидаем нормального попадания
                            {
                                generateDigits();
                            }
                        }

                        if (digits[i, j] == 9)    // вляпались
                        {
                            explosion(i, j);
                        }
                        else  // всё норм, не вляпались
                        {
                            openNeighbours(i, j);  // открываем соседей
                        }
                    }
                }
                else     // ПКМ
                {
                    toggleFlag(i, j);
                }

                checkWin();   // проверка выигрыша
            }

            
            // теперь проверяем нажатие на морду
            y += offset;
            if (x >= face_x && x <= face_x + face_len && y >= face_y && y <= face_y + face_len)
            {
                face = 1;
                face_click = true;
            }

            Invalidate();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void лёгкий9х910МинToolStripMenuItem_Click(object sender, EventArgs e)
        {
            M = 9;
            N = 9;
            quanOfMines = 10;
            newGame();
        }

        private void любительToolStripMenuItem_Click(object sender, EventArgs e)
        {
            M = 16;
            N = 16;
            quanOfMines = 40;
            newGame();
        }

        private void профессионал16х3099МинToolStripMenuItem_Click(object sender, EventArgs e)
        {
            M = 30;
            N = 16;
            quanOfMines = 99;
            newGame();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timer < 999)
            {
                timer++;
            }

            label2.Text = timer.ToString("D3");  // выводим время
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if(face == 1){  // если рот был открыт, закрываем
                face = 0;
            }
            face_click = false;
            Invalidate();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            if (!first_click)
            {
                timer1.Start();
            }
        }
    }
}
