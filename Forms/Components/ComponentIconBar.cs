using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace GenieClient
{
    public partial class ComponentIconBar
    {
        public ComponentIconBar()
        {
            InitializeComponent();
        }

        private Genie.Globals m_Globals = null;
        private object m_Lock = new object();

        public Genie.Globals Globals
        {
            get
            {
                return m_Globals;
            }

            set
            {
                m_Globals = value;
            }
        }

        private bool m_IsConnected = false;

        public bool IsConnected
        {
            get
            {
                return m_IsConnected;
            }

            set
            {
                m_IsConnected = value;
                UpdateStatusIcons();
                PictureBoxCompass.Invalidate();
            }
        }

        private void UpdateStatusIcons()
        {
            var argpb = PictureBoxStatus;
            UpdateImage(argpb);
            var argpb1 = PictureBoxStunned;
            UpdateImage(argpb1);
            var argpb2 = PictureBoxBleeding;
            UpdateImage(argpb2);
            var argpb3 = PictureBoxInvisible;
            UpdateImage(argpb3);
            var argpb4 = PictureBoxHidden;
            UpdateImage(argpb4);
            var argpb5 = PictureBoxJoined;
            UpdateImage(argpb5);
            var argpb6 = PictureBoxWebbed;
            UpdateImage(argpb6);
        }

        private void UpdateImage(PictureBox pb)
        {
            if (m_IsConnected == true)
            {
                if (!Information.IsNothing(pb.Tag) && pb.Tag.ToString().EndsWith("_gray") == true)
                {
                    ShowImage(pb, pb.Tag.ToString().Substring(0, pb.Tag.ToString().Length - 5));
                }
            }
            else if (!Information.IsNothing(pb.Tag) && pb.Tag.ToString().EndsWith("_gray") == false)
            {
                ShowImage(pb, pb.Tag.ToString() + "_gray");
            }
        }

        private void ComponentIconBar_Load(object sender, EventArgs e)
        {
            // Load icons from embedded resources
            LoadEmbeddedIcons();
            
            // Also try to load from file system as fallback
            string iconsPath = LocalDirectory.Path + @"\Icons\";
            if (Directory.Exists(iconsPath))
            {
                var oFiles = new DirectoryInfo(iconsPath).GetFiles("*.png");
                foreach (FileInfo fi in oFiles)
                    AddImage(fi.Name);
            }
            
            var argp = PictureBoxCompass;
            ShowImage(argp, "compass.png");
        }

        private void LoadEmbeddedIcons()
        {
            // Load compass icons from embedded resources
            AddEmbeddedImage("compass.png", My.Resources.Resources.compass);
            AddEmbeddedImage("compass_north.png", My.Resources.Resources.compass_north);
            AddEmbeddedImage("compass_northeast.png", My.Resources.Resources.compass_northeast);
            AddEmbeddedImage("compass_east.png", My.Resources.Resources.compass_east);
            AddEmbeddedImage("compass_southeast.png", My.Resources.Resources.compass_southeast);
            AddEmbeddedImage("compass_south.png", My.Resources.Resources.compass_south);
            AddEmbeddedImage("compass_southwest.png", My.Resources.Resources.compass_southwest);
            AddEmbeddedImage("compass_west.png", My.Resources.Resources.compass_west);
            AddEmbeddedImage("compass_northwest.png", My.Resources.Resources.compass_northwest);
            AddEmbeddedImage("compass_up.png", My.Resources.Resources.compass_up);
            AddEmbeddedImage("compass_down.png", My.Resources.Resources.compass_down);
            AddEmbeddedImage("compass_out.png", My.Resources.Resources.compass_out);
            
            // Load status icons from embedded resources
            AddEmbeddedImage("standing.png", My.Resources.Resources.standing);
            AddEmbeddedImage("sitting.png", My.Resources.Resources.sitting);
            AddEmbeddedImage("kneeling.png", My.Resources.Resources.kneeling);
            AddEmbeddedImage("prone.png", My.Resources.Resources.prone);
            AddEmbeddedImage("dead.png", My.Resources.Resources.dead);
            AddEmbeddedImage("stunned.png", My.Resources.Resources.stunned);
            AddEmbeddedImage("bleeding.png", My.Resources.Resources.bleeding);
            AddEmbeddedImage("invisible.png", My.Resources.Resources.invisible);
            AddEmbeddedImage("hidden.png", My.Resources.Resources.hidden);
            AddEmbeddedImage("joined.png", My.Resources.Resources.joined);
            AddEmbeddedImage("webbed.png", My.Resources.Resources.webbed);
        }

        private void AddEmbeddedImage(string sName, Bitmap image)
        {
            if (image != null && !ImageListIcons.Images.ContainsKey(sName))
            {
                ImageListIcons.Images.Add(sName, image);
                ImageListIcons.Images.Add(sName + "_gray", ImageToGrayScale(image));
            }
        }

        private void AddImage(string sName)
        {
            string sPathName = LocalDirectory.Path + @"\Icons\" + sName;
            if (File.Exists(sPathName))
            {
                ImageListIcons.Images.Add(sName, Image.FromFile(sPathName));
                Bitmap argb = (Bitmap)Image.FromFile(sPathName);
                ImageListIcons.Images.Add(sName + "_gray", ImageToGrayScale(argb));
            }
        }

        private Bitmap ImageToGrayScale(Bitmap b)
        {
            // int iColor = 0;
            var oOutput = new Bitmap(b.Width, b.Height);
            for (int X = 0, loopTo = b.Width - 1; X <= loopTo; X++)
            {
                for (int Y = 0, loopTo1 = b.Height - 1; Y <= loopTo1; Y++)
                    oOutput.SetPixel(X, Y, Genie.ColorCodeWindows.ColorToGrayscale(b.GetPixel(X, Y)));
            }

            return oOutput;
        }

        private void AppendImage(Graphics g, string sName)
        {
            if (m_IsConnected == false)
            {
                sName += "_gray";
            }

            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (!Information.IsNothing(ImageListIcons.Images[sName]))
                    {
                        Bitmap b = (Bitmap)ImageListIcons.Images[sName];
                        b.MakeTransparent(Color.Black);
                        g.DrawImage(b, 0, 0);
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        private void ShowImage(PictureBox p, string sName)
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_IsConnected == false)
                    {
                        if (sName.EndsWith("_gray") == false)
                        {
                            sName += "_gray";
                        }
                    }

                    SetImage(p, sName);
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        // Threadsafe
        private void SetImage(PictureBox p, string sName)
        {
            if (p.InvokeRequired == true)
            {
                var parameters = new object[] { p, sName };
                Invoke(new InvokeSetImageDelegate(InvokeSetImage), parameters);
            }
            else
            {
                InvokeSetImage(p, sName);
            }
        }

        public delegate void InvokeSetImageDelegate(PictureBox p, string sName);

        private void InvokeSetImage(PictureBox p, string sName)
        {
            if (Monitor.TryEnter(ImageListIcons))
            {
                try
                {
                    if (!Information.IsNothing(ImageListIcons.Images[sName]))
                    {
                        p.Image = ImageListIcons.Images[sName];
                        p.Tag = sName;
                    }
                }
                finally
                {
                    Monitor.Exit(ImageListIcons);
                }
            }
        }

        public void UpdateStatusBox()
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_Globals.VariableList["dead"]?.ToString() == "1")
                    {
                        var argp = PictureBoxStatus;
                        ShowImage(argp, "dead.png");
                    }
                    else if (m_Globals.VariableList["standing"]?.ToString() == "1")
                    {
                        var argp1 = PictureBoxStatus;
                        ShowImage(argp1, "standing.png");
                    }
                    else if (m_Globals.VariableList["kneeling"]?.ToString() == "1")
                    {
                        var argp4 = PictureBoxStatus;
                        ShowImage(argp4, "kneeling.png");
                    }
                    else if (m_Globals.VariableList["sitting"]?.ToString() == "1")
                    {
                        var argp3 = PictureBoxStatus;
                        ShowImage(argp3, "sitting.png");
                    }
                    else if (m_Globals.VariableList["prone"]?.ToString() == "1")
                    {
                        var argp2 = PictureBoxStatus;
                        ShowImage(argp2, "prone.png");
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        public void UpdateStunned()
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_Globals.VariableList["stunned"]?.ToString() == "1")
                    {
                        var argp = PictureBoxStunned;
                        ShowImage(argp, "stunned.png");
                    }
                    else
                    {
                        PictureBoxStunned.Image = null;
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        public void UpdateBleeding()
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_Globals.VariableList["bleeding"]?.ToString() == "1")
                    {
                        var argp = PictureBoxBleeding;
                        ShowImage(argp, "bleeding.png");
                    }
                    else
                    {
                        PictureBoxBleeding.Image = null;
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        public void UpdateInvisible()
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_Globals.VariableList["invisible"]?.ToString() == "1")
                    {
                        var argp = PictureBoxInvisible;
                        ShowImage(argp, "invisible.png");
                    }
                    else
                    {
                        PictureBoxInvisible.Image = null;
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        public void UpdateHidden()
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_Globals.VariableList["hidden"]?.ToString() == "1")
                    {
                        var argp = PictureBoxHidden;
                        ShowImage(argp, "hidden.png");
                    }
                    else
                    {
                        PictureBoxHidden.Image = null;
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        public void UpdateJoined()
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_Globals.VariableList["joined"]?.ToString() == "1")
                    {
                        var argp = PictureBoxJoined;
                        ShowImage(argp, "joined.png");
                    }
                    else
                    {
                        PictureBoxJoined.Image = null;
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        public void UpdateWebbed()
        {
            if (Monitor.TryEnter(m_Lock))
            {
                try
                {
                    if (m_Globals.VariableList["webbed"]?.ToString() == "1")
                    {
                        var argp = PictureBoxWebbed;
                        ShowImage(argp, "webbed.png");
                    }
                    else
                    {
                        PictureBoxWebbed.Image = null;
                    }
                }
                finally
                {
                    Monitor.Exit(m_Lock);
                }
            }
        }

        private void PictureBoxCompass_Paint(object sender, PaintEventArgs e)
        {
            if (!Information.IsNothing(m_Globals))
            {
                if (m_Globals.VariableList["north"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_north.png");
                }

                if (m_Globals.VariableList["northeast"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_northeast.png");
                }

                if (m_Globals.VariableList["east"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_east.png");
                }

                if (m_Globals.VariableList["southeast"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_southeast.png");
                }

                if (m_Globals.VariableList["south"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_south.png");
                }

                if (m_Globals.VariableList["southwest"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_southwest.png");
                }

                if (m_Globals.VariableList["west"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_west.png");
                }

                if (m_Globals.VariableList["northwest"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_northwest.png");
                }

                if (m_Globals.VariableList["up"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_up.png");
                }

                if (m_Globals.VariableList["down"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_down.png");
                }

                if (m_Globals.VariableList["out"]?.ToString() == "1")
                {
                    DrawCompassImage(e.Graphics, "compass_out.png");
                }
            }
        }

        // Simple helper to draw compass image without locks
        private void DrawCompassImage(Graphics g, string sName)
        {
            if (m_IsConnected == false)
            {
                sName = sName.Replace(".png", "_gray.png");
                // Also try without the replacement in case gray version doesn't exist
                if (Information.IsNothing(ImageListIcons.Images[sName]))
                {
                    sName = sName.Replace("_gray.png", ".png") + "_gray";
                }
            }
            
            if (!Information.IsNothing(ImageListIcons.Images[sName]))
            {
                Bitmap b = (Bitmap)ImageListIcons.Images[sName];
                b.MakeTransparent(Color.Black);
                g.DrawImage(b, 0, 0);
            }
        }
    }
}
