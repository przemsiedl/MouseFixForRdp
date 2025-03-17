// OverlayCursorForm.cs
/// <summary>
/// Form nakładki – wyświetla obrazek kursora. Jest bezramkowy, przezroczysty i zawsze na wierzchu.
/// </summary>
public class OverlayCursorForm : Form
{
    private PictureBox pictureBox;
    public OverlayCursorForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.Manual;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.BackColor = Color.Lime;
        this.TransparencyKey = Color.Lime;
        this.Size = new Size(32, 32);
        pictureBox = new PictureBox();
        pictureBox.Size = this.Size;
        pictureBox.Location = new Point(0, 0);
        pictureBox.BackColor = Color.Transparent;

        try
        {
            pictureBox.Image = Image.FromFile("cursor.png");
        }
        catch (Exception)
        {
            Bitmap bmp = new Bitmap(this.Width, this.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawLine(Pens.Black, 0, 0, this.Width, this.Height);
                g.DrawLine(Pens.Black, this.Width, 0, 0, this.Height);
            }
            pictureBox.Image = bmp;
        }

        this.Controls.Add(pictureBox);
    }
}
