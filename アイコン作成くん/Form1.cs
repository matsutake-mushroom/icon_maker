using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace アイコン作成くん
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            pictureBox1.AllowDrop = true;
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
        }

        private void PictureBox1_DragDrop(object sender, DragEventArgs e)
        {
            string[] fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            string fileName = fileNames[0];
            pictureBox1.ImageLocation = fileName;
            textBox1.Text = fileName;
        }

        private void PictureBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                int size = Convert.ToInt32(textBox2.Text);
                label_size.Text = "x " + size.ToString();

                if (size <= 0 || size > 256)
                {
                    checkBox0.Checked = false;
                    checkBox0.Enabled = false;
                    throw new Exception("サイズは1～256で指定してください。");
                }

                checkBox0.Enabled = true;

            }
            catch (Exception ee)
            {
                checkBox0.Checked = false;
                checkBox0.Enabled = false;
                MessageBox.Show(ee.Message);
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Image img = null;
            try
            {
                img = Image.FromFile(pictureBox1.ImageLocation);
            }
            catch (Exception exc)
            {
                MessageBox.Show("ファイルを開けませんでした。\n\n" + exc.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            bool check = false;

            var data_array = new List<IconData>();

            if (checkBox16.Checked)
            {
                data_array.Add(new IconData(16,img));
                check = true;
            }
            if (checkBox32.Checked)
            {
                data_array.Add(new IconData(32,img));
                check = true;
            }
            if (checkBox48.Checked)
            {
                data_array.Add(new IconData(48,img));
                check = true;
            }
            if (checkBox64.Checked)
            {
                data_array.Add(new IconData(64,img));
                check = true;
            }
            if (checkBox128.Checked)
            {
                data_array.Add(new IconData(128,img));
                check = true;
            }
            if (checkBox256.Checked)
            {
                data_array.Add(new IconData(256,img));
                check = true;
            }
            if (checkBox0.Checked)
            {
                data_array.Add(new IconData(Convert.ToInt32(textBox2.Text),img));
                check = true;
            }

            if (!check)
            {
                MessageBox.Show("サイズを選択してください。");
                return;
            }
            //データ
            List<byte> ret = new List<byte>();

            //ヘッダ作成

            byte[] header = new byte[6];
            header[0] = 0x00;//reserved
            header[1] = 0x00;//
            header[2] = 0x01;//ICOファイル指定
            header[3] = 0x00;//
            int n_img = data_array.Count;
            header[4] = (byte) (n_img);
            header[5] = (byte) (n_img >> 8);

            ret.AddRange(header);

            //ディレクトリヘッダ作成
            uint counter = (uint)header.Length;
            counter += (uint)(16 * data_array.Count);//ヘッダ長さ固定＊個数

            foreach (var icon in data_array)
            {
                icon.setDataOffset(counter);//ディレクトリヘッダを完成させる
                ret.AddRange(icon.header);//ヘッダを書き込む
                counter += (uint)icon.img_data.Length;
            }

            //データ部作成
            foreach (var icon in data_array)
            {
                ret.AddRange(icon.img_data);
            }

            var dir = Path.GetDirectoryName(pictureBox1.ImageLocation);
            var fname = Path.GetFileNameWithoutExtension(pictureBox1.ImageLocation);
            var filename = Path.Combine(dir, fname + ".ico");

            using (var fd = new SaveFileDialog())
            {
                fd.RestoreDirectory = true;
                fd.InitialDirectory = dir;
                fd.Title = "保存先・保存ファイル名を選んでください。";
                fd.FileName = fname + ".ico";
                    fd.Filter = "Iconファイル(*.ico)|*.ico|すべてのファイル(*.*)|*.*";

                if (fd.ShowDialog() == DialogResult.OK)
                {
                    filename = fd.FileName;
                }
                else
                {
                    return;
                }
            }

            

            

            try
            {
                File.WriteAllBytes(filename, ret.ToArray());
                MessageBox.Show("アイコンファイル\n" + filename + "\nを作成しました。");
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "エラー");
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            using (var fd = new OpenFileDialog())
            {
                fd.CheckFileExists = true;
                fd.Title = "画像ファイルを選択してください。";

                if (fd.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.ImageLocation = fd.FileName;
                }
            }
        }
    }


    public class IconData
    {
        private int width;
        private int height;
        public byte[] header;
        public byte[] img_data;

        public IconData(int size, Image src)
        {
            width = size;
            height = size;

            header = new byte[16];

            header[0] = (byte) width;
            header[1] = (byte) height;

            header[2] = 0x00;//カラーパレットを使わない
            header[3] = 0x00;//reserved

            header[4] = 0x01;//ICOフォーマット
            header[5] = 0x00;//

            header[6] = 0x00;//PNGを使うのでbit深度は指定しない
            header[7] = 0x00;//

            using (Bitmap bitmap = new Bitmap(src, width, height)) {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    img_data = stream.ToArray();
                }
            }

            uint n_bytes_img = (uint)img_data.Length;
            header[8] = (byte) n_bytes_img;
            header[9] = (byte) (n_bytes_img >> 8);
            header[10] = (byte)(n_bytes_img >> 16);
            header[11] = (byte)(n_bytes_img >> 24);

        }

        public void setDataOffset(uint num)
        {
            header[12] = (byte)num;
            header[13] = (byte)(num >> 8);
            header[14] = (byte)(num >> 16);
            header[15] = (byte)(num >> 24);
        }


    }
}
