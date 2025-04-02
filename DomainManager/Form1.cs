using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using System.Data;
using System.DirectoryServices;

namespace DomainManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void ListDomainUsers(string ip, string domainName, string username, string password, DateTime? filteredDate = null)
        {
            try
            {
                dataGridView1.DataSource = null;
                dataGridView1.Rows.Clear();
                dataGridView1.Columns.Clear();

                DataTable table = new DataTable();
                table.Columns.Add("Username", typeof(string));
                table.Columns.Add("Last Logon", typeof(string));

                string baseDn = ConvertDomainToBaseDn(domainName);
                string ldapPath = $"LDAP://{ip}/{baseDn}";

                DirectoryEntry entry = new DirectoryEntry(ldapPath, username, password);
                DirectorySearcher searcher = new DirectorySearcher(entry);

                searcher.Filter = "(&(objectClass=user)(objectCategory=person))";
                searcher.PropertiesToLoad.Add("samaccountname");
                searcher.PropertiesToLoad.Add("lastlogon");

                SearchResultCollection results = searcher.FindAll();
                foreach (SearchResult result in results)
                {
                    string? dcUsername = result.Properties["samaccountname"].Count > 0 ? result.Properties["samaccountname"][0].ToString() : "Unknown Username";
                    long lastLogon = result.Properties["lastLogon"].Count > 0 ? (long)result.Properties["lastLogon"][0] : 0;

                    DateTime lastLogonDate = DateTime.FromFileTime(lastLogon);

                    if (filteredDate.HasValue && lastLogonDate >= filteredDate.Value) continue;

                    string lastLogonDisplay = lastLogonDate == DateTime.MinValue ? "Hiç giriş yapmadı" : lastLogonDate.ToString("yyyy-MM-dd HH:mm");

                    table.Rows.Add(dcUsername, lastLogonDisplay);
                }

                dataGridView1.DataSource = table;
                dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        private string ConvertDomainToBaseDn(string domainName)
        {
            var parts = domainName.Split('.');
            return string.Join(",", parts.Select(p => $"DC={p}"));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ListDomainUsers(textBox1.Text, textBox2.Text, textBox3.Text, textBox4.Text, dateTimePicker1.Value);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using ExcelPackage excel = new ExcelPackage();
            var worksheet = excel.Workbook.Worksheets.Add("Sheet1");

            for (int col = 0; col < dataGridView1.Columns.Count; col++)
            {
                worksheet.Cells[1, col + 1].Value = dataGridView1.Columns[col].HeaderText;
            }

            for (int row = 0; row < dataGridView1.Rows.Count; row++)
            {
                for (int col = 0; col < dataGridView1.Columns.Count; col++)
                {
                    var value = dataGridView1.Rows[row].Cells[col].Value;
                    worksheet.Cells[row + 2, col + 1].Value = value?.ToString();
                }
            }

            using SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
            saveFileDialog.Title = "Excel dosyasını kaydet";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileInfo fi = new FileInfo(saveFileDialog.FileName);
                excel.SaveAs(fi);
                MessageBox.Show("✅ Excel dosyası başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
