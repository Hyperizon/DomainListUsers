using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using System.DirectoryServices;

namespace DomainManager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Dictionary<string, DateTime> userDictionary = new Dictionary<string, DateTime>();

        private void ListDomainUsers(string ip, string domainName, DateTime? filteredDate = null)
        {
            try
            {
                listBox1.Items.Clear();

                DirectoryEntry entry = new DirectoryEntry($"LDAP://{ip}/DC={domainName},DC=local");
                DirectorySearcher searcher = new DirectorySearcher(entry);

                searcher.Filter = "(&(objectClass=user)(objectCategory=person))";
                searcher.PropertiesToLoad.Add("samaccountname");
                searcher.PropertiesToLoad.Add("lastlogon");

                SearchResultCollection results = searcher.FindAll();
                foreach (SearchResult result in results)
                {
                    string? username = result.Properties["samaccountname"].Count > 0 ? result.Properties["samaccountname"][0].ToString() : "Unknown Username";
                    long lastLogon = result.Properties["lastLogon"].Count > 0 ? (long)result.Properties["lastLogon"][0] : 0;

                    DateTime lastLogonDate = DateTime.FromFileTime(lastLogon);

                    if (filteredDate.HasValue && lastLogonDate >= filteredDate.Value) continue;

                    listBox1.Items.Add(username!);
                    userDictionary.TryAdd(username!, lastLogonDate);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ListDomainUsers(textBox1.Text, textBox2.Text, dateTimePicker1.Value);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using ExcelPackage excel = new ExcelPackage();
            var worksheet = excel.Workbook.Worksheets.Add("Sheet1");

            worksheet.Cells[1,1].Value = "Username";
            worksheet.Cells[1, 2].Value = "Last Logon";

            int row = 2;

            for (int i = 0; i <= userDictionary.Count; i++)
            {
                foreach (var user in userDictionary)
                {
                    worksheet.Cells[row, 1].Value = $"{user.Key}";
                    worksheet.Cells[row, 2].Value = $"{user.Value}";
                    row++;
                }
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel files|*.xlsx";
            saveFileDialog.Title = "Save an Excel File";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                excel.SaveAs(new FileInfo(saveFileDialog.FileName));
            }
        }
    }
}
