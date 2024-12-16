using System.DirectoryServices;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace UserProfile
{
    public partial class UpdateProfile : Form
    {
        private SearchResult _user;
        private string samAccountName;
        private bool isProduction = false;
        private bool isCtrlPressed = false;
        private bool isShiftPressed = false;
        private bool isQPressed = false;


        // Import the FindWindow and ShowWindow functions from user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, uint Msg);

        // Constants for ShowWindow
        private const uint SW_HIDE = 0;
        private const uint SW_SHOW = 5;

        public string UserDn { get; private set; }

        public UpdateProfile()
        {
            InitializeComponent();
            this.KeyPreview = true;

            IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);

            // Hide the taskbar
            if (taskbarHandle != IntPtr.Zero)
            {
                ShowWindow(taskbarHandle, SW_HIDE);
            }
        }
        #region experimentalCode
        private void UpdateProfile_Load(object sender, EventArgs e)
        {
            LoadUserProfile();
        }
  
        #endregion
        private void LoadUserProfile()
        {
            // Get the current Windows user
            string currentUser = WindowsIdentity.GetCurrent().Name;
            string[] userDomain = currentUser.Split('\\');
            if (userDomain.Length != 2)
            {
                MessageBox.Show("Unable to get the current user information.");
                return;
            }

            string domainPath = MyWindowsServer.domainPath;
            string name = MyWindowsServer.name;
            string password = MyWindowsServer.password;
            isProduction = MyWindowsServer.isProduction;

            //DirectoryEntry directoryEntry = new DirectoryEntry(domainPath, name, password);
            DirectoryEntry directoryEntry = new DirectoryEntry(domainPath, name, password);
            DirectorySearcher ds = new DirectorySearcher(directoryEntry);
            ds.Filter = $"(samaccountname={userDomain[1]})";
            samAccountName = userDomain[1];
            //ds.Filter = $"(objectclass=*)";

            try
            {
                _user = ds.FindOne();
                if (_user == null)
                {
                    MessageBox.Show("User not found in Active Directory.");
                    return;
                }

                DisplayUserProperties();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving user: {ex.Message}");
            }
        }
        private void DisplayUserProperties()
        {
            var properties = _user.Properties;

            // Display properties in the text boxes
            labelName.Text = properties["displayname"].Count > 0 ? properties["displayname"][0].ToString() : string.Empty;
            labelEmail.Text = properties["mail"].Count > 0 ? properties["mail"][0].ToString() : (properties["userprincipalname"].Count > 0 ? properties["userprincipalname"][0].ToString() : string.Empty);
            //textBoxDepartment.Text = properties["department"].Count > 0 ? properties["department"][0].ToString() : string.Empty;
            string selectedDevision = properties["department"].Count > 0 ? properties["department"][0].ToString() : string.Empty;
            comboBoxDivision.SelectedItem = selectedDevision;
            textBoxDepartment.Text = selectedDevision;
            //cbBoxFunction();
            string selectUnitOrGroup = properties["unit"].Count > 0 ? properties["unit"][0].ToString() : string.Empty;
            comboBoxUnit.SelectedItem = selectUnitOrGroup;
            textBoxUnit.Text = selectUnitOrGroup;
            textBoxCompany.Text = properties["company"].Count > 0 ? properties["company"][0].ToString() : string.Empty;
            textBoxJobTitle.Text = properties["title"].Count > 0 ? properties["title"][0].ToString() : string.Empty;
            ShowAndUpdateProductionProperties(properties);
            //foreach (string key in properties.PropertyNames)
            //{
            //    var values = properties[key];
            //    foreach (var value in values)
            //    {
            //        File.AppendAllText("result.txt", $"{key}: {value}{Environment.NewLine}");
            //    }
            //}
        }


        

        

        private void ShowAndUpdateProductionProperties(ResultPropertyCollection properties)
        {
            labelOfficeLocation.Visible = isProduction;
            textBoxOfficeLocation.Visible = isProduction;
            labelPersonalMobile.Visible = isProduction;
            textBoxPersonalMobile.Visible = isProduction;
            labelBusinessMobile.Visible = isProduction;
            labelInfoBusinessMobile.Visible = isProduction;
            textBoxBusinessMobile.Visible = isProduction;
            labelTelephoneNumber.Visible = isProduction;
            textBoxTelephoneNumber.Visible = isProduction;
            labelHideMobile.Visible = isProduction;
            checkBoxHideMobile.Visible = isProduction;
            textBoxBusinessMobile.Text = properties["mobileotp"].Count > 0 ? properties["mobileotp"][0].ToString() : string.Empty;
            textBoxOfficeLocation.Text = properties["physicaldeliveryofficename"].Count > 0 ? properties["physicaldeliveryofficename"][0].ToString() : string.Empty;
            checkBoxHideMobile.Checked = properties["mobileotphide"].Count > 0 ? (properties["mobileotphide"][0].ToString().ToLower() == "true" ? true : false) : true;
            if (checkBoxHideMobile.Checked)
            {
                //labelBusinessMobile.Visible = false;
                //textBoxBusinessMobile.Visible = false;

                labelPersonalMobile.Visible = false;
                textBoxPersonalMobile.Visible = false;
            }
            else
            {
                textBoxPersonalMobile.Text = properties["mobile"].Count > 0 ? properties["mobile"][0].ToString() : string.Empty;
            }
            textBoxTelephoneNumber.Text = properties["telephonenumber"].Count > 0 ? properties["telephonenumber"][0].ToString() : string.Empty;
        }


        private void buttonUpdateProfile_Click(object sender, EventArgs e)
        {
            if (!checkBoxHideMobile.Checked && !IsValidCountryCode(textBoxPersonalMobile.Text))
            {
                MessageBox.Show("Please fill country code for Personal Mobile");
            }
            else if (!IsValidCountryCode(textBoxBusinessMobile.Text))
            {
                MessageBox.Show("Please fill country code for OTP/Business Mobile");
            }
            else
            {
                string title = textBoxJobTitle.Text;
                string company = textBoxCompany.Text;
                string department = comboBoxDivision.SelectedItem.ToString(); //textBoxDepartment.Text;
                string unitOrGroup = comboBoxUnit.SelectedItem.ToString();
                string office = textBoxOfficeLocation.Text;
                string mobile = textBoxPersonalMobile.Text;
                string mobileOtp = textBoxBusinessMobile.Text;
                string mobileHideOtp = checkBoxHideMobile.Checked ? "true" : "false";
                string telephoneNumber = textBoxTelephoneNumber.Text;
                try
                {
                    // storing a log record in the database
                    using (var context = new UserProfileContext())
                    {

                        User user = new User
                        {
                            SamAccountName = samAccountName,
                            JobTitle = title,
                            Company = company,
                            Department = department,
                            Unit = unitOrGroup,
                            Office = office,
                            PersonalMobile =  mobile,
                            MobileOtp = mobileOtp,
                            TelephoneNumber = telephoneNumber,
                            CreationDate = DateTime.Now,
                            DisplayName = "APP",
                            Email = ""
                        };

                        context.Users.Add(user);
                        context.SaveChanges();
                    }

                    DirectoryEntry userEntry = _user.GetDirectoryEntry();
                    if (userEntry.Properties.Contains("title"))
                    {
                        userEntry.Properties["title"].Value = title; //Ok
                    }
                    else
                    {
                        userEntry.Properties["title"].Add(title);
                    }

                    if (userEntry.Properties.Contains("company"))
                    {
                        userEntry.Properties["company"].Value = company; //OK
                    }
                    else
                    {
                        userEntry.Properties["company"].Add(company);
                    }

                    if (userEntry.Properties.Contains("department"))
                    {
                        userEntry.Properties["department"].Value = department; //OK
                    }
                    else
                    {
                        userEntry.Properties["department"].Add(department);
                    }

                    if (userEntry.Properties.Contains("unit"))
                    {
                        userEntry.Properties["unit"].Value = unitOrGroup;
                    }
                    else
                    {
                        userEntry.Properties["unit"].Add(unitOrGroup);
                    }

                    if (isProduction)
                    {
                        if (userEntry.Properties.Contains("physicalDeliveryOfficeName"))
                        {
                            userEntry.Properties["physicalDeliveryOfficeName"].Value = office;
                        }
                        else
                        {
                            userEntry.Properties["physicalDeliveryOfficeName"].Add(office);
                        }

                        if (userEntry.Properties.Contains("mobileOTP"))
                        {
                            userEntry.Properties["mobileOTP"].Value = mobileOtp;
                        }
                        else
                        {
                            userEntry.Properties["mobileOTP"].Add(mobileOtp);
                        }


                        if (userEntry.Properties.Contains("mobileOTPhide"))
                        {
                            userEntry.Properties["mobileOTPhide"].Value = mobileHideOtp;
                        }
                        else
                        {
                            userEntry.Properties["mobileOTPhide"].Add(mobileHideOtp);
                        }

                        if (!checkBoxHideMobile.Checked)
                        {
                            if (userEntry.Properties.Contains("mobile"))
                            {
                                userEntry.Properties["mobile"].Value = mobile; //OK
                            }
                            else
                            {
                                userEntry.Properties["mobile"].Add(mobile);
                            }
                        }
                        else
                        {
                            if (userEntry.Properties.Contains("mobile"))
                            {
                                userEntry.Properties["mobile"].Value = "<not set>"; //OK
                            }
                            else
                            {
                                userEntry.Properties["mobile"].Add("<not set>");
                            }
                        }
                        if (textBoxTelephoneNumber.Text == "")
                        {
                            if (userEntry.Properties.Contains("telephoneNumber"))
                            {
                                userEntry.Properties["telephoneNumber"].Value = "<not set>"; // OK
                            }
                            else
                            {
                                userEntry.Properties["telephoneNumber"].Add("<not set>");
                            }
                        }
                        else
                        {
                            if (userEntry.Properties.Contains("telephoneNumber"))
                            {
                                userEntry.Properties["telephoneNumber"].Value = telephoneNumber; // OK
                            }
                            else
                            {
                                userEntry.Properties["telephoneNumber"].Add(telephoneNumber);
                            }
                        }
                    }
                    userEntry.CommitChanges();

                  

                    if (comboBoxDivision.SelectedItem != textBoxDepartment.Text)
                    {
                        string oldGroupDn = "";
                        string newGroupDn = "";
                        string oldDivisionDn = "";
                        string newDivisionDn = "";

                        //
                        //
                        //start move to other ou
                        //
                        // Get the current user's SamAccountName
                        //

                        string currentUserName = Environment.UserName;
                        string sAMAccountName = samAccountName;
                        try
                        {

                            if (userEntry.Properties.Contains("cn"))
                            {
                                currentUserName = userEntry.Properties["cn"].Value.ToString();
                            }
                            if (userEntry.Properties.Contains("sAMAccountName"))
                            {
                                sAMAccountName = userEntry.Properties["sAMAccountName"].Value.ToString();
                            }

                        }
                        catch (Exception ex) { }
                        // Initialize userDN and targetOUDN
                        string userDistinguishedName = "";
                        string targetOUDistinguishedName = "";

                        // Get the current user's distinguished name dynamically
                        string commonName = currentUserName.Replace("\\", "\\\\").Replace(",", "\\,");

                        // Unit/Group Old Value - Determine the current OU


                        switch (textBoxUnit.Text)
                        {

                            // Under Program Division Old OU

                            case string s when s.Contains("Creativity & Innovation Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Creativity & Innovation Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Knowledge & Learning Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Knowledge & Learning Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Library Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Library Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Museum & Exhibits Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Museum & Exhibits Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Performing Arts Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Performing Arts Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            // Under Operation Division Old OU

                            case string s when s.Contains("Concessionaire Management Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Concessionaire Management Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Scheduling & Events Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Scheduling & Events Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Visitor Experience Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Visitor Experience Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Volunteer Services Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Volunteer Services Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            // Under Technical Services Old OU

                            case string s when s.Contains("Application  Management Group"):
                                userDistinguishedName = $"CN={commonName},OU=Application  Management Group,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Audio Visual Group"):
                                userDistinguishedName = $"CN={commonName},OU=Technology & Service Management,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Computing & Communication Management Group"):
                                userDistinguishedName = $"CN={commonName},OU=Computing & Communication Management Group,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Information Protection Group"):
                                userDistinguishedName = $"CN={commonName},OU=Information Protection Group,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Facility Operation & Maintenance Group"):
                                userDistinguishedName = $"CN={commonName},OU=Facility Operation & Maintenance Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("IT Command Center"):
                                userDistinguishedName = $"CN={commonName},OU=Command Center Users,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            // Under Communication & Partnerships Division Old OU

                            case string s when s.Contains("Communications Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Communications Unit,OU=Communication & Partnerships Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Marketing & Branding Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Marketing & Branding Unit,OU=Communication & Partnerships Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("Relations & Partnership Group"):
                                userDistinguishedName = $"CN={commonName},OU=Relations & Partnership Group,OU=Communication & Partnerships Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            // Under Others

                            case string s when s.Contains("Planning & Support Unit"):
                                userDistinguishedName = $"CN={commonName},OU=Planning & Support Unit,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("OE & Compliance Group"):
                                userDistinguishedName = $"CN={commonName},OU=OE & Compliance Group,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;
                                
                            case string s when s.Contains("ITHRA Digital Wellness Signature Prgm Group"):
                                userDistinguishedName = $"CN={commonName},OU=ITHRA Digital Wellness Signature Program,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case string s when s.Contains("National Values & Identity Program Group"):
                                userDistinguishedName = $"CN={commonName},OU=National Values & Identity Program,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            default:
                                MessageBox.Show("Unit not recognized.");
                                return; // Exit if the unit is not recognized
                        }

                        userDistinguishedName = ActiveDirectoryHelper.GetDistinguishedName( samAccountName, "User");

                        //
                        //
                        // New target OU based on the new unit
                        //
                        //
                        switch (comboBoxUnit.SelectedItem) // Assuming you have a new unit input for the target OU
                        {

                            // Under Program Division New OU

                            case "Creativity & Innovation Unit":
                                targetOUDistinguishedName = "OU=Creativity & Innovation Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Knowledge & Learning Unit":
                                targetOUDistinguishedName = "OU=Knowledge & Learning Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Library Unit":
                                targetOUDistinguishedName = "OU=Library Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Museum & Exhibits Unit":
                                targetOUDistinguishedName = "OU=Museum & Exhibits Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Performing Arts Unit":
                                targetOUDistinguishedName = "OU=Performing Arts Unit,OU=Programs Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            // Under Operation Division New OU

                            case "Concessionaire Management Unit":
                                targetOUDistinguishedName = "OU=Concessionaire Management Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Scheduling & Events Unit":
                                targetOUDistinguishedName = "OU=Scheduling & Events Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Visitor Experience Unit":
                                targetOUDistinguishedName = "OU=Visitor Experience Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Volunteer Services Unit":
                                targetOUDistinguishedName = "OU=Volunteer Services Unit,OU=Ithra Operations Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            // Under Technical Services New OU

                            case "Application  Management Group":
                                targetOUDistinguishedName = "OU=Application  Management Group,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Audio Visual Group":
                                targetOUDistinguishedName = "OU=Technology & Service Management,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Computing & Communication Management Group":
                                targetOUDistinguishedName = "OU=Computing & Communication Management Group,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Information Protection Group":
                                targetOUDistinguishedName = "OU=Information Protection Group,OU=Information Technology Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Facility Operation & Maintenance Group":
                                targetOUDistinguishedName = "OU=Facility Operation & Maintenance Section,OU=Technical Services Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "IT Command Center":
                                targetOUDistinguishedName = "OU=Command Center Users,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;
                            // Under Communication & Partnerships Division New OU

                            case "Communications Unit":
                                targetOUDistinguishedName = "OU=Communications Unit,OU=Communication & Partnerships Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Marketing & Branding Unit":
                                targetOUDistinguishedName = "OU=Marketing & Branding Unit,OU=Communication & Partnerships Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "Relations & Partnership Group":
                                targetOUDistinguishedName = "OU=Relations & Partnership Group,OU=Communication & Partnerships Division,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            // Under Others New OU


                            case "Planning & Support Unit":
                                targetOUDistinguishedName = "OU=Planning & Support Unit,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "OE & Compliance Group":
                                targetOUDistinguishedName = "OU=OE & Compliance Group,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "ITHRA Digital Wellness Signature Prgm Group":
                                targetOUDistinguishedName = "OU=ITHRA Digital Wellness Signature Program,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;

                            case "National Values & Identity Program Group":
                                targetOUDistinguishedName = "OU=National Values & Identity Program,OU=KACWC USERS,DC=kacwc,DC=org";
                                break;


                            default:
                                MessageBox.Show("Target Unit not recognized.");
                                return; // Exit if the target unit is not recognized
                        }

                        // Now move the user from the current OU to the target OU
                        ActiveDirectoryHelper.MoveUserToOU(userDistinguishedName, targetOUDistinguishedName);



                        //end move to other ou


                        
                        // removing group member and adding new member
                        //Department Old value
                        switch (textBoxDepartment.Text)
                        {
                            case string s when s.Contains("Programs Division"):
                                oldDivisionDn = "SG.30019294 Programs Division";
                                break;
                            case string s when s.Contains("Ithra Operations Division"):
                                oldDivisionDn = "SG.30020229 Ithra Operations Division";
                                break;
                            case string s when s.Contains("Technical Services Division"):
                                oldDivisionDn = "SG.30023559 Technical Services Division";
                                break;
                            case string s when s.Contains("Communication & Partnership Division"):
                                oldDivisionDn = "SG.30026915 Communication & Partnerships Division";
                                break;
                            case string s when s.Contains("Others"):
                                oldDivisionDn = "SG.30018208 King Abdulaziz Center for world Culture (Ithra)";
                                break;
                        }

                        //Unit/Group Old Value
                        switch (textBoxUnit.Text)
                        {

                            // Under Program Division Old Value
                            case string s when s.Contains("Creativity & Innovation Unit"):
                                oldGroupDn = "SG.30019294 Programs Division_Creativity & Innovation Unit";
                                break;
                            case string s when s.Contains("Knowledge & Learning Unit"):
                                oldGroupDn = "SG.30019294 Programs Division_Knowledge & Learning Unit";
                                break;
                            case string s when s.Contains("Library Unit"):
                                oldGroupDn = "SG.30019294 Programs Division_Library Unit";
                                break;
                            case string s when s.Contains("Museum & Exhibits Unit"):
                                oldGroupDn = "SG.30019294 Programs Division_Museum & Exhibits Unit";
                                break;
                            case string s when s.Contains("Performing Arts Unit"):
                                oldGroupDn = "SG.30019294 Programs Division_Performing Arts Unit";
                                break;

                            // Under Operation Division Old Value
                            case string s when s.Contains("Concessionaire Management Unit"):
                                oldGroupDn = "SG.30020229 Ithra Operations Division_Concessionaire Management";
                                break;
                            case string s when s.Contains("Scheduling & Events Unit"):
                                oldGroupDn = "SG.30020229 Ithra Operations Division_Scheduling & Events";
                                break;
                            case string s when s.Contains("Visitor Experience Unit"):
                                oldGroupDn = "SG.30020229 Ithra Operations Division_Visitor Experience Unit";
                                break;
                            case string s when s.Contains("Volunteer Services Unit"):
                                oldGroupDn = "SG.30020229 Ithra Operations Division_Volunteer Services";
                                break;

                            // Under Technical Services Division
                            case string s when s.Contains("Application  Management Group"):
                                oldGroupDn = "SG.30023559 Technical Services Division_AMG";
                                break;
                            case string s when s.Contains("Audio Visual Group"):
                                oldGroupDn = "SG.30023559 Technical Services Division_AV";
                                break;
                            case string s when s.Contains("Computing & Communication Management Group"):
                                oldGroupDn = "SG.30023559 Technical Services Division_CCG";
                                break;
                            case string s when s.Contains("Information Protection Group"):
                                oldGroupDn = "SG.30023559 Technical Services Division IPG";
                                break;
                            case string s when s.Contains("Facility Operation & Maintenance Group"):
                                oldGroupDn = "SG.30023559 Technical Services Division_Facility";
                                break;
                            case string s when s.Contains("IT Command Center"):
                                oldGroupDn = "SG.30023559 Technical Services Division_Facility";
                                break;
                            // Under Communication & Partnerships Division
                            case string s when s.Contains("Communications Unit"):
                                oldGroupDn = "SG.30026915 Comm & Part Division_Communications Unit";
                                break;
                            case string s when s.Contains("Marketing & Branding Unit"):
                                oldGroupDn = "SG.30026915 Comm & Part Division_Marketing & Branding Unit";
                                break;
                            case string s when s.Contains("Relations & Partnership Group"):
                                oldGroupDn = "SG.30026915 Comm & Part Division_Relations & Partnerships Group";
                                break;

                            // Under Others
                            case string s when s.Contains("Planning & Support Unit"):
                                oldGroupDn = "SG.30020227 Planning & Support Unit";
                                break;
                            case string s when s.Contains("OE & Compliance Group"):
                                oldGroupDn = "SG.30035928 OE & Compliance Group";
                                break;
                            case string s when s.Contains("ITHRA Digital Wellness Signature Prgm Group"):
                                oldGroupDn = "SG.30040276 ITHRA Digital Wellness Signature Program";
                                break;
                            case string s when s.Contains("National Values & Identity Program Group"):
                                oldGroupDn = "SG.30047977 National Values & Identity Program";
                                break;
                        }

                        //New Unit/Group
                        switch (comboBoxUnit.SelectedItem)
                        {
                            // Under Program Division New Value
                            case "Creativity & Innovation Unit":
                                newGroupDn = "SG.30019294 Programs Division_Creativity & Innovation Unit";
                                break;
                            case "Knowledge & Learning Unit":
                                newGroupDn = "SG.30019294 Programs Division_Knowledge & Learning Unit";
                                break;
                            case "Library Unit":
                                newGroupDn = "SG.30019294 Programs Division_Library Unit";
                                break;
                            case "Museum & Exhibits Unit":
                                newGroupDn = "SG.30019294 Programs Division_Museum & Exhibits Unit";
                                break;
                            case "Performing Arts Unit":
                                newGroupDn = "SG.30019294 Programs Division_Performing Arts Unit";
                                break;

                            // Under Operation Division New Value
                            case "Concessionaire Management Unit":
                                newGroupDn = "SG.30020229 Ithra Operations Division_Concessionaire Management";
                                break;
                            case "Scheduling & Events Unit":
                                newGroupDn = "SG.30020229 Ithra Operations Division_Scheduling & Events";
                                break;
                            case "Visitor Experience Unit":
                                newGroupDn = "SG.30020229 Ithra Operations Division_Visitor Experience Unit";
                                break;
                            case "Volunteer Services Unit":
                                newGroupDn = "SG.30020229 Ithra Operations Division_Volunteer Services";
                                break;

                            // Under Technical Services Division
                            case "Application  Management Group":
                                newGroupDn = "SG.30023559 Technical Services Division_AMG";
                                break;
                            case "Audio Visual Group":
                                newGroupDn = "SG.30023559 Technical Services Division_AV";
                                break;
                            case "Computing & Communication Management Group":
                                newGroupDn = "SG.30023559 Technical Services Division_CCG";
                                break;
                            case "Information Protection Group":
                                newGroupDn = "SG.30023559 Technical Services Division IPG";
                                break;
                            case "Facility Operation & Maintenance Group":
                                newGroupDn = "SG.30023559 Technical Services Division_Facility";
                                break;
                            case "IT Command Center":
                                newGroupDn = "SG.30023559 Technical Services Division_CCG";
                                break;

                            // Under Communication & Partnerships Division
                            case "Communications Unit":
                                newGroupDn = "SG.30026915 Comm & Part Division_Communications Unit";
                                break;
                            case "Marketing & Branding Unit":
                                newGroupDn = "SG.30026915 Comm & Part Division_Marketing & Branding Unit";
                                break;
                            case "Relations & Partnership Group":
                                newGroupDn = "SG.30026915 Comm & Part Division_Relations & Partnerships Group";
                                break;

                            // Under Others
                            case "Planning & Support Unit":
                                newGroupDn = "SG.30020227 Planning & Support Unit";
                                break;

                            case "OE & Compliance Group":
                                newGroupDn = "SG.30035928 OE & Compliance Group";
                                break;

                            case "ITHRA Digital Wellness Signature Prgm Group":
                                newGroupDn = "SG.30040276 ITHRA Digital Wellness Signature Program";
                                break;

                            case "National Values & Identity Program Group":
                                newGroupDn = "SG.30047977 National Values & Identity Program";
                                break;
                        }



                        //New Division
                        switch (comboBoxDivision.SelectedItem)
                        {
                            case "Programs Division":
                                newDivisionDn = "SG.30019294 Programs Division";
                                break;
                            case "Ithra Operations Division":
                                newDivisionDn = "SG.30020229 Ithra Operations Division";
                                break;
                            case "Technical Services Division":
                                newDivisionDn = "SG.30023559 Technical Services Division";
                                break;
                            case "Communication & Partnership Division":
                                newDivisionDn = "SG.30026915 Communication & Partnerships Division";
                                break;
                            case "Others":
                                newDivisionDn = "SG.30018208 King Abdulaziz Center for world Culture (Ithra)";
                                break;
                        }

                        // end removing group member and adding new member

                        //Update Division
                        //if (!string.IsNullOrEmpty(oldDivisionDn) && !string.IsNullOrEmpty(newDivisionDn))
                        {
                            UpdateMemberOfValue(userEntry.Properties["sAMAccountName"].Value.ToString(), oldDivisionDn, newDivisionDn);
                        }
                        //Update Unit/Group
                        //if (!string.IsNullOrEmpty(oldGroupDn) && !string.IsNullOrEmpty(newGroupDn))
                        {
                            UpdateMemberOfValue(userEntry.Properties["sAMAccountName"].Value.ToString(), oldGroupDn, newGroupDn);
                        }


                    }


                    if (comboBoxUnit.SelectedItem != textBoxUnit.Text)
                    {
                        SendEmailToManager(userEntry.Properties["sAMAccountName"].Value.ToString());
                    }

                    LogUpdate(userEntry.Properties["sAMAccountName"].Value.ToString()); // OK
                    MessageBox.Show("User profile updated successfully");

                    isCtrlPressed = true;
                    isShiftPressed = true;
                    isQPressed = true;
                    IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                    // Show the taskbar
                    if (taskbarHandle != IntPtr.Zero)
                    {
                        ShowWindow(taskbarHandle, SW_SHOW);
                    }
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }


        private void UpdateMemberOfValue(string userCn, string groupCn, string newGroupCn)
        {
            string userDn = ActiveDirectoryHelper.GetDistinguishedName(userCn, "user");
            string groupDn = ActiveDirectoryHelper.GetDistinguishedName(groupCn, "group");
            string newGroupDn = ActiveDirectoryHelper.GetDistinguishedName(newGroupCn, "group");
            ActiveDirectoryHelper.ModifyUserGroupMembership(userDn, groupDn, newGroupDn);
            
        }


        // new Code end

        private void SendEmailToManager(string WindowUserNameX)
        {
            using (var context = new UserProfileContext())
            {
                var selectedUnit = comboBoxUnit.SelectedItem.ToString();
                var group = context.Group.FirstOrDefault(g => g.Name == selectedUnit);

                if (group == null)
                {
                    MessageBox.Show("Group not found in database.", "Error");
                    return;
                }

                String sendText = "The user below recently updated their profile information. This action has granted access to Unit/ Group data" + "\n\n";
                sendText += "Name: " + labelName.Text + "\n";
                sendText += "Username: " + WindowUserNameX + "\n";
                sendText += "Mobile: " + textBoxBusinessMobile.Text + "\n";
                sendText += "Old Department: " + textBoxDepartment.Text + "\n";
                sendText += "New Department: " + comboBoxDivision.SelectedItem + "\n";

                string ManagerEmailAddress = group.ManagerEmail;

                try
                {
                    string UserEmailAddressX = WindowUserNameX + "@ithra.com";

                    var smtpClient = new SmtpClient("10.172.30.60")
                    {
                        Port = 25,
                        Credentials = new NetworkCredential(UserEmailAddressX, ""),
                        EnableSsl = false,
                    };

                    smtpClient.Send(UserEmailAddressX, ManagerEmailAddress,
                        "Unit/Group Updated!!! User Login -" + WindowUserNameX,
                        sendText);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to send email.", "Error");
                }
            }
        }


        private void LogUpdate(string username)
        {
            //string logPath = "UserProfileUpdates.csv";
            //if (!File.Exists(logPath))
            //{
            //    using (StreamWriter sw = new StreamWriter(logPath))
            //    {
            //        sw.WriteLine("Datetime, UserName, Company, Division, Unit/Group, JobTitle, Location, HideMobile, PersonalMobile, BusinessMobile, TelephoneNumber");
            //    }
            //}
            //string logEntry = $"{DateTime.Now}, {username}, {textBoxCompany.Text}, {comboBoxDivision.SelectedItem}, {comboBoxUnit.SelectedItem}, {textBoxJobTitle.Text}, {textBoxOfficeLocation.Text}, {checkBoxHideMobile.Checked}, {textBoxPersonalMobile.Text}, {textBoxBusinessMobile.Text}, {textBoxTelephoneNumber.Text}";
            //File.AppendAllText(logPath, logEntry + Environment.NewLine);
        }

        private void checkBoxHideMobile_CheckedChanged(object sender, EventArgs e)
        {
            
            labelPersonalMobile.Visible = !checkBoxHideMobile.Checked;
            textBoxPersonalMobile.Visible = !checkBoxHideMobile.Checked;
            if (!checkBoxHideMobile.Checked)
            {
                textBoxPersonalMobile.Text = "";
            }
        }

        private void checkBoxAgreement_CheckedChanged(object sender, EventArgs e)
        {
            buttonUpdateProfile.Enabled = checkBoxAgreement.Checked;
        }

        private void comboBoxDivision_SelectedIndexChanged(object sender, EventArgs e)
        {
            cbBoxFunction();
        }

        private void cbBoxFunction()
        {
            //Ithra Program Division
            if (comboBoxDivision.SelectedItem.ToString() == "Programs Division")
            {
                // Under Program Division Unit
                comboBoxUnit.Items.Clear();
                comboBoxUnit.Items.Add("Creativity & Innovation Unit");
                comboBoxUnit.Items.Add("Knowledge & Learning Unit");
                comboBoxUnit.Items.Add("Library Unit");
                comboBoxUnit.Items.Add("Museum & Exhibits Unit");
                comboBoxUnit.Items.Add("Performing Arts Unit");
                comboBoxUnit.SelectedIndex = 0;
            }

            // Ithra Operation Division
            else if (comboBoxDivision.SelectedItem.ToString() == "Ithra Operations Division")
            {
                // Under Operation Unit
                comboBoxUnit.Items.Clear();
                comboBoxUnit.Items.Add("Concessionaire Management Unit");
                comboBoxUnit.Items.Add("Scheduling & Events Unit");
                comboBoxUnit.Items.Add("Visitor Experience Unit");
                comboBoxUnit.Items.Add("Volunteer Services Unit");
                comboBoxUnit.SelectedIndex = 0;
            }

            // Ithra Technical Services Division
            else if (comboBoxDivision.SelectedItem.ToString() == "Technical Services Division")
            {
                // Under Technical Services Division
                comboBoxUnit.Items.Clear();
                comboBoxUnit.Items.Add("Facility Operation & Maintenance Group");
                comboBoxUnit.Items.Add("Information Protection Group");
                comboBoxUnit.Items.Add("Computing & Communication Management Group");
                comboBoxUnit.Items.Add("Application  Management Group");
                comboBoxUnit.Items.Add("Audio Visual Group");
                comboBoxUnit.Items.Add("IT Command Center");
                comboBoxUnit.SelectedIndex = 0;
            }

            // Ithra Communication & Partnership Division
            else if (comboBoxDivision.SelectedItem.ToString() == "Communication & Partnership Division")
            {
                // Under Commnication & Partnership Division
                comboBoxUnit.Items.Clear();
                comboBoxUnit.Items.Add("Communications Unit");
                comboBoxUnit.Items.Add("Marketing & Branding Unit");
                comboBoxUnit.Items.Add("Relations & Partnership Group");
                comboBoxUnit.SelectedIndex = 0;
            }
            else if (comboBoxDivision.SelectedItem.ToString() == "Others")
            {
                comboBoxUnit.Items.Clear();
                comboBoxUnit.Items.Add("Planning & Support Unit");
                comboBoxUnit.Items.Add("OE & Compliance Group");
                comboBoxUnit.Items.Add("ITHRA Digital Wellness Signature Prgm Group");
                comboBoxUnit.Items.Add("National Values & Identity Program Group");
                comboBoxUnit.SelectedIndex = 0;
            }
            else
            {
                comboBoxUnit.Items.Clear();
            }
        }

        static bool IsValidCountryCode(string contactNumber)
        {
            string pattern = @"^\+\d{1,3}";

            return Regex.IsMatch(contactNumber, pattern);
        }

        static string FormatPhoneNumber(string phoneNumber)
        {
            if (phoneNumber.StartsWith("0"))
            {
                phoneNumber = phoneNumber.Substring(1);
                return "+966" + phoneNumber;
            }
            else
            {
                return phoneNumber;
            }
        }

        private void textBoxBusinessMobile_MouseLeave(object sender, EventArgs e)
        {
            string phoneNumber = textBoxBusinessMobile.Text;
            textBoxBusinessMobile.Text = FormatPhoneNumber(phoneNumber);
        }

        private void textBoxPersonalMobile_MouseLeave(object sender, EventArgs e)
        {
            string phoneNumber = textBoxPersonalMobile.Text;
            textBoxPersonalMobile.Text = FormatPhoneNumber(phoneNumber);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void UpdateProfile_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isCtrlPressed && isShiftPressed && isQPressed)
            {
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                // Show the taskbar
                if (taskbarHandle != IntPtr.Zero)
                {
                    ShowWindow(taskbarHandle, SW_SHOW);
                }
                Application.Exit();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void UpdateProfile_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
                isCtrlPressed = true;
            if (e.KeyCode == Keys.ShiftKey)
                isShiftPressed = true;

            if (isCtrlPressed && isShiftPressed && e.KeyCode == Keys.Q)
            {
                isQPressed = true;
                this.Close();
            }
        }

        private void UpdateProfile_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey)
                isCtrlPressed = false;
            if (e.KeyCode == Keys.ShiftKey)
                isShiftPressed = false;
            isQPressed = false;
        }
    }
}