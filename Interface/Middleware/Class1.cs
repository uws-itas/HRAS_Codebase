﻿using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Configuration;
using System.Collections.Specialized;
using System.Data;
using System.Security.Cryptography;
using System.Threading;

namespace Middleware
{
	public enum PrivilegeLevels
	{
		NONE = 0,
		R = 1,
		A = 2,
	}

	public enum ImportType
	{
		MEDICAL = 0,
		INVENTORY = 1,
		ROOM = 2,
		USERS = 4
	}

    public enum DataLength
    {
        //INVENTORY
        STOCKID = 5,
        QUANTITY = 5,
        DESCRIPTION = 35,
        SIZE = 3,
        COST = 8,
        //MEDICAL RECORD
        LASTNAME = 50,
        FIRSTNAME = 25,
        MIDDLEINITIAL = 1,
        GENDER = 1,
        SSN = 9,
        BIRTHDATE = 8,
        ENTRYDATETIME = 12,
        EXITDATETIME = 12,
		ATTENDINGPHY = 5,
        ROOMNO = 9,
        SYMPTOM1 = 25,
        SYMPTOM2 = 25,
        SYMPTOM3 = 25,
        SYMPTOM4 = 25,
        SYMPTOM5 = 25,
        SYMPTOM6 = 25,
        DIAGNOSIS = 75,
        NOTES = 100,
        INSURER = 5,
        ADDRESSLINE1 = 35,
        ADDRESSLINE2 = 35,
        ADDRESSCITY = 25,
        ADDRESSSTATE = 2,
        ADDRESSZIP = 5,
        DNRSTATUS = 1,
        ORGANDONOR = 1,
		//ROOM
		ROOMNUMBER = 9,
		HOURLYRATE = 5,
		EFFECTIVEDATE = 8,
		//USERS
		USERNAME = 25,
		PASSWORD = 50,
		USERTYPE = 1
    }

    public enum DataStart
    {
        //INVENTORY
        STOCKID = 0,
        QUANTITY = DataLength.STOCKID,
        DESCRIPTION = DataLength.STOCKID + DataLength.QUANTITY,
        SIZE = DataLength.STOCKID + DataLength.QUANTITY + DataLength.DESCRIPTION,
        COST = DataLength.STOCKID + DataLength.QUANTITY + DataLength.DESCRIPTION + DataLength.SIZE,
        //MEDICAL RECORD
        LASTNAME = 0,
        FIRSTNAME = DataLength.LASTNAME,
        MIDDLEINITIAL = FIRSTNAME + DataLength.FIRSTNAME,
        GENDER = MIDDLEINITIAL + DataLength.MIDDLEINITIAL,
        SSN = GENDER + DataLength.GENDER,
        BIRTHDATE = SSN + DataLength.SSN,
        ENTRYDATETIME = BIRTHDATE + DataLength.BIRTHDATE,
        EXITDATETIME = ENTRYDATETIME + DataLength.ENTRYDATETIME,
        ATTENDINGPHY = EXITDATETIME + DataLength.EXITDATETIME,
        ROOMNO = ATTENDINGPHY + DataLength.ATTENDINGPHY,
        SYMPTOM1 = ROOMNO + DataLength.ROOMNO,
        SYMPTOM2 = SYMPTOM1 + DataLength.SYMPTOM1,
        SYMPTOM3 = SYMPTOM2 + DataLength.SYMPTOM2,
        SYMPTOM4 = SYMPTOM3 + DataLength.SYMPTOM3,
        SYMPTOM5 = SYMPTOM4 + DataLength.SYMPTOM4,
        SYMPTOM6 = SYMPTOM5 + DataLength.SYMPTOM5,
        DIAGNOSIS = SYMPTOM6 + DataLength.SYMPTOM6,
        NOTES = DIAGNOSIS + DataLength.DIAGNOSIS,
        INSURER = NOTES + DataLength.NOTES,
        ADDRESSLINE1 = INSURER + DataLength.INSURER,
        ADDRESSLINE2 = ADDRESSLINE1 + DataLength.ADDRESSLINE1,
        ADDRESSCITY = ADDRESSLINE2 + DataLength.ADDRESSLINE2,
        ADDRESSSTATE = ADDRESSCITY + DataLength.ADDRESSCITY,
        ADDRESSZIP = ADDRESSSTATE + DataLength.ADDRESSSTATE,
        DNRSTATUS = ADDRESSZIP + DataLength.ADDRESSZIP,
        ORGANDONOR = DNRSTATUS + DataLength.DNRSTATUS,
		//ROOM
		ROOMNUMBER = 0,
		HOURLYRATE = ROOMNUMBER + DataLength.ROOMNUMBER,
		EFFECTIVEDATE = HOURLYRATE + DataLength.HOURLYRATE,
		//USERS
		USERNAME = 0,
		PASSWORD = DataLength.USERNAME + USERNAME,
		USERTYPE = DataLength.PASSWORD + PASSWORD
	}

    public class Session
	{
		SqlConnection conn;
		User currentUser;
        static Session theSession;
		public static Exception failedConnectionException = new Exception("A connection to the database could not be established.");


		private Session(string username, string password)
		{
			try
			{
                string connectionString = Properties.Settings1.Default.CONNECTIONSTRING;
                conn = new SqlConnection(connectionString);
				conn.Open();
				currentUser = new User();
				if (!currentUser.login(username, password, conn))
				{
					throw User.failedLoginException;
				}
			}
			catch (Exception e)
			{
				throw e;
			}
		}

        public static Session establishSession(string username, string password)
        {
            if (theSession == null)
            {
				try
				{
					theSession = new Session(username, password);
				}
				catch (Exception e)
				{
					throw e;
				}
			}
            return theSession;
        }

        public static Session getCurrentSession()
        {
            return theSession;
        }

        public SqlConnection getConnection()
		{
			return conn;
		}

		public void closeConnection()
		{
			conn.Close();
            theSession = null;
		}

		public bool verifySession()
		{
			return currentUser.isLoggedIn();
		}

		public User getCurrentUser()
		{
			return currentUser;
		}
	}

    public class DiagnosisWizardMid
    {
        string SymptomList = "WHERE Symptom_Name <> ''";
        string DiagnosisList = "WHERE Symptom_Name <> ''";
        bool exit = false;
        public string answer;

        public void RunDiagnosisWizard()
        {
            while (!exit)// connect exit to the exit button to leave the diagnosis wizard
            {
                string fiftyName = getSymptomName(SymptomList);
                bool response = Response(answer);

                if (response == true)
                {
                    DiagnosisList += (" OR Symptom_Name = '" + fiftyName + "'");
                }
                else
                {
                   DiagnosisList += (" OR Symptom_Name <> '" + fiftyName + "'");
                }

                SymptomList += (" AND Symptom_Name <> '" + fiftyName + "'");
            }
        }

        public string getSymptomList()
        {
            return SymptomList;
        }

        public string askQuestion(string name)
        {
            return "Is the patient  show signs of " + name + "?";
        }

        public bool Response(string answer)
        {
            if (answer == "Yes")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string[,] getDiagnosisPercentages(string DiagnosisList)
        {
            string DiagnosisScript = "SELECT TOP 10 * FROM(" +
                "SELECT Diagnosis, (COUNT(Diagnosis) *100)/" +
                "(SELECT COUNT(Diagnosis) " +
                "FROM(Visited_History inner join Show_signs " +
                "ON Show_signs.Patient_SSN = Visited_History.Patient_SSN AND Show_signs.Entry_Date = Visited_History.Entry_Date)" +
                DiagnosisList +
                ") AS Diagnosis_Percentage " +
                "FROM(Visited_History inner join Show_signs " +
                "ON Show_signs.Patient_SSN = Visited_History.Patient_SSN AND Show_signs.Entry_Date = Visited_History.Entry_Date) " +
                DiagnosisList +
                "Group By Diagnosis " +
                ") AS DiagnosisTempTable " +
                "Where DiagnosisTempTable.Diagnosis_Percentage<> 0 " +
                "ORDER BY DiagnosisTempTable.Diagnosis_Percentage DESC;";

            SqlCommand querySymptom = new SqlCommand(DiagnosisScript);
            querySymptom.CommandType = System.Data.CommandType.Text;
            SqlDataReader DiagnosisPercents = querySymptom.ExecuteReader();
            

            string[,] PercentsList = new string[10,2];
            int i = 0;
            while(DiagnosisPercents.Read())
            {
                PercentsList[i, 0] = DiagnosisPercents.GetString(0);
                PercentsList[i, 1] = DiagnosisPercents.GetString(1);
                i++;
            }
            return PercentsList;
        }

        public string getSymptomName(string SymptomList)
        {
            string SymptomScript = "SELECT TOP 1 Symptom_Name FROM(" +
                "Select Symptom_Name, ABS(500-(COUNT(Symptom_Name) *1000/" +
                "(SELECT COUNT(Symptom_Name) " +
                "FROM Show_Signs " +
                SymptomList +
                "))) AS SymptomNumber " +
                "FROM Show_signs " +
                SymptomList +
                "Group By Symptom_Name " +
                ") As SymptomTempTable " +
                "ORDER BY SymptomTempTable.SymptomNumber ASC;";

            SqlCommand querySymptom = new SqlCommand(SymptomScript);
            querySymptom.CommandType = System.Data.CommandType.Text;
            SqlDataReader SymNameReader = querySymptom.ExecuteReader();
            SymNameReader.Read();

            return SymNameReader.GetString(0);
        }
	}

	public class Diagnosis
	{
		string diagnosisName;
		List<string> symptoms = new List<string>();

		public Diagnosis(string name)
		{
			diagnosisName = name;
		}

		public string getName()
		{
			return diagnosisName;
		}

		public void addSymptom(string symptom)
		{
			symptoms.Add(symptom);
		}

		public List<string> getSymptoms()
		{
			return symptoms;
		}

		public static string getSymptoms(string ssn, DateTime entryDate)
		{
			string symptoms = "";
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Retrieve_Symptoms";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@ssn", ssn));
			command.Parameters.Add(new SqlParameter("@entryDate", entryDate));
			SqlDataReader dataReaderRate = command.ExecuteReader();
			while (dataReaderRate.Read())
			{
				int index = dataReaderRate.GetOrdinal("Symptom_Name");
				symptoms += (dataReaderRate.GetString(index)) + ",";
			}
			dataReaderRate.Close();
			return symptoms;

		}
	}

	public class PasswordHasher
	{
		public static string hashPassword(string pass)
		{
			byte[] passwordBytes = Encoding.ASCII.GetBytes(pass);
			HashAlgorithm sha = new SHA1CryptoServiceProvider();
			byte[] hashedBytes = sha.ComputeHash(passwordBytes);
			return Convert.ToBase64String(hashedBytes);
		}
	}

	public class User
	{
		string username;
		string password;
		int privilegeLevel;
		bool loggedIn = false;
		public static Exception failedLoginException = new Exception("Incorrect username or password.");
		public static Exception noAccountException = new Exception("The username given does not exist.");
		public static Exception accountLockedException = new Exception("The specified account is locked.");
		int loginAttemptThreshold = 5;

		public bool login(string enteredUsername, string enteredPassword, SqlConnection connection)
		{
			bool matchFound = false;
			string queryString = "Verify_Username";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@userName", enteredUsername));
			SqlDataReader dataReader = command.ExecuteReader();
			bool userExists = false;
			while (dataReader.Read())
			{
				userExists = true;
			}
			if (!userExists) throw noAccountException;
			else
			{
				int loginAttempts = getFailedAttempts(enteredUsername);
				if (loginAttempts >= loginAttemptThreshold) throw accountLockedException;
			}
			queryString = "Verify_Login";
			command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@username", enteredUsername));
			command.Parameters.Add(new SqlParameter("@password", enteredPassword));
			dataReader.Close();
			dataReader = command.ExecuteReader();
			while (dataReader.Read())
			{
				int indexUsername = dataReader.GetOrdinal("User_Name");
				string retrievedUsername = dataReader.GetString(indexUsername);
				if (retrievedUsername == enteredUsername) // not necessary, but in case of hacker mischief
				{
					matchFound = true;
					username = enteredUsername;
					password = enteredPassword;
					int index = dataReader.GetOrdinal("User_Type");
					string privilege = dataReader.GetString(index);
					privilegeLevel = (int)(Enum.Parse(typeof(PrivilegeLevels), privilege));
					loggedIn = true;
				}
				else throw failedLoginException;
			}
			dataReader.Close();
			command.Dispose();

			return matchFound;
		}

		public static int getFailedAttempts(string userName)
		{
			string connectionString = Properties.Settings1.Default.CONNECTIONSTRING;
			SqlConnection connection = new SqlConnection(connectionString);
			connection.Open();
			string queryString = "Get_Failed_Attempts"; 
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@userName", userName));
			SqlDataReader dataReader = command.ExecuteReader();
			int indexAttempts = dataReader.GetOrdinal("Failed_Login");
			int fails = 5;
			while (dataReader.Read())
			{
				fails = dataReader.GetInt32(indexAttempts);
			}
			connection.Close();
			return fails;
		}



		public bool logout()
		{
			try
			{
				username = "";
				password = "";
				privilegeLevel = (int)PrivilegeLevels.NONE; // this is ok becuase PrivilegeLevels is an enumeration thus the values are actually ints
				//logoffTimer = null;
				loggedIn = false;
				Session currentSession = Session.getCurrentSession();
				currentSession.closeConnection();
				return true;
			}
			catch (Exception e)
			{
				return false;
			}
		}

		public bool isLoggedIn()
		{
			return loggedIn;
		}

        public string getUsername()
        {
            return username;
        }

		public int getPrivilegeLevel()
		{
			return privilegeLevel;
		}
	}

	public class BasicAddress
	{
		private string addressLineOne;
		private string addressLineTwo;
		private string city;
		private string state;
		private string zip;

        public BasicAddress(string line1, string line2, string theCity, string theState, string theZip)
        {
			addressLineOne = line1;
			addressLineTwo = line2;
			city = theCity;
			state = theState;
			zip = theZip;
        }

		public string getAddressLineOne()
		{
			return addressLineOne;
		}
		public string getAddressLineTwo()
		{
			return addressLineTwo;
		}
		public string getCity()
		{
			return city;
		}
		public string getState()
		{
			return state;
		}
		public string getZip()
		{
			return zip;
		}

	}

	public class Patient
	{
		string lastName;
		string firstName;
		char middleInitial;
		char gender;
		string ssn;
		DateTime birthDate;
		BasicAddress address;
		bool dnrStatus;
		bool organDonor;
		public static Exception noPatient = new Exception("No patient with the provided ssn is checked in.");

        public Patient(string theLastName, string theFirstName, char middle, char theGender, string SSN, DateTime birthdate, BasicAddress theAddress, bool theDnrStatus, bool Donor)
        {
			lastName = theLastName;
			firstName = theFirstName;
			middleInitial = middle;
			gender = theGender;
			ssn = SSN;
			birthDate = birthdate;
			address = theAddress;
			dnrStatus = theDnrStatus;
			organDonor = Donor;
        }

		public string getFirstName()
		{
			return firstName;
		}

		public string getLastName()
		{
			return lastName;
		}

		public char getMiddleInitial()
		{
			return middleInitial;
		}

		public string getSSN()
		{
			return ssn;
		}

		public DateTime getBirthDate()
		{
			return birthDate;
		}

		public BasicAddress getAddress()
		{
			return address;
		}

		public char getGender()
		{
			return gender;
		}

		public bool getDnrStatus()
		{
			return dnrStatus;
		}

		public bool getOrganDonor()
		{
			return organDonor;
		}

		public static bool checkPatient(string ssn)
		{
			SqlConnection connection = Session.getCurrentSession().getConnection();
			bool alreadyExists = false;
			string queryString = "Retrieve_SSN";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@ssn", ssn));
			SqlDataReader dataReader = command.ExecuteReader();
			while (dataReader.Read())
			{
				alreadyExists = true;
			}
			dataReader.Close();
			return alreadyExists;
		}

		public static DateTime getEntryDate(string ssn)
		{
			bool entryExists = false;
			DateTime entryDate = DateTime.Now;
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Get_Entry_Date";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@ssn", ssn));
			SqlDataReader dataReader = command.ExecuteReader();
			while (dataReader.Read())
			{
				int index = dataReader.GetOrdinal("Entry_Date");
				entryDate = dataReader.GetDateTime(index);
				entryExists = true;
			}
			dataReader.Close();
			if (!entryExists) throw noPatient;
			return entryDate;
		}

        public static bool checkInPatient(string FirstName, string LastName, string MiddleInitial, string SSN, string Birthdate, string Gender, string Address, string City, string State, string Zip)
        {
            SqlConnection connection = Session.getCurrentSession().getConnection();
            bool successful = false;
            string queryString = "CheckIn_Patient";
            SqlCommand command = new SqlCommand(queryString, connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@firstName", FirstName));
            command.Parameters.Add(new SqlParameter("@lastName", LastName));
            command.Parameters.Add(new SqlParameter("@middleInitial", MiddleInitial));
            command.Parameters.Add(new SqlParameter("@ssn", SSN));
            command.Parameters.Add(new SqlParameter("@birthdate", Birthdate));
            command.Parameters.Add(new SqlParameter("@address", Address));
			command.Parameters.Add(new SqlParameter("@addressState", State));
			command.Parameters.Add(new SqlParameter("@addressCity", City));
            command.Parameters.Add(new SqlParameter("@addressZip", Zip));
			command.Parameters.Add(new SqlParameter("@gender", Gender));
			try
			{
				command.ExecuteNonQuery();
				successful = true;
			}
			catch (Exception)
			{
				return false;
			}
            return successful;
            //need a procedure for this
        }
        public static bool checkInExistedPatient(string SSN)
        {
            SqlConnection connection = Session.getCurrentSession().getConnection();
            bool successful = false;
            string queryString = "CheckInExisted_Patient";
            SqlCommand command = new SqlCommand(queryString, connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@ssn", SSN));
			try
			{
				command.ExecuteNonQuery();
				successful = true;
			}
			catch (Exception)
			{
				return false;
			}
            return successful;
            //need a procedure for this
        }

        public static bool patientExists(string SSN)
        {
            SqlConnection connection = Session.getCurrentSession().getConnection();
            bool alreadyExists = false;
            string queryString = "Retrieve_Patient";
            SqlCommand command = new SqlCommand(queryString, connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@SSN", SSN));
            SqlDataReader dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                alreadyExists = true;
            }
            dataReader.Close();
            return alreadyExists;
            //need a procedure for this
        }

    }

	public class MedicalRecord
	{
		Patient patient;
		DateTime entryDate;
		DateTime exitDate;
		string attendingPhysician = "";
		Dictionary<Room, double> previousRooms;
		Room currentRoom;
		Diagnosis diagnosis;
		string notes;
		string insurer;
		List<InventoryItem> suppliesUsed;

		public MedicalRecord(string ssn, DateTime date)
		{
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Retrieve_Patient_Page";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@ssn", ssn));
			command.Parameters.Add(new SqlParameter("@entryDateTime", date));
			SqlDataReader dataReader = command.ExecuteReader();
			while (dataReader.Read())
			{
				int indexFirstName = dataReader.GetOrdinal("First_Name");
				int indexLastName = dataReader.GetOrdinal("Last_Name");
				int indexMiddleInitial = dataReader.GetOrdinal("Middle_initial");
				int indexGender = dataReader.GetOrdinal("Gender");
				int indexBirthDate = dataReader.GetOrdinal("Birth_Date");
				int indexAddressLine1 = dataReader.GetOrdinal("Address_Line1");
				int indexAddressLine2 = dataReader.GetOrdinal("Address_Line2");
				int indexAddressCity = dataReader.GetOrdinal("Address_City");
				int indexAddressState = dataReader.GetOrdinal("Address_State");
				int indexAddressZip = dataReader.GetOrdinal("Address_Zip");
				int indexDNR = dataReader.GetOrdinal("DNR_Status");
				int indexDonor = dataReader.GetOrdinal("Organ_Donor");
				int indexEntryDate = dataReader.GetOrdinal("Entry_Date");
				int indexExitDate = dataReader.GetOrdinal("Exit_Date");
				int indexDiagnosis = dataReader.GetOrdinal("Diagnosis");
				int indexNotes = dataReader.GetOrdinal("Notes");
				int indexInsurer = dataReader.GetOrdinal("Insurer");
				string firstName = dataReader.GetString(indexFirstName);
				string lastName = dataReader.GetString(indexLastName);
				string middleInitial = dataReader.GetString(indexMiddleInitial);
				string gender = dataReader.GetString(indexGender);
				DateTime birthDate = DateTime.ParseExact(dataReader.GetString(indexBirthDate), "MMddyyyy", null);
				bool dnr = (dataReader.GetString(indexDNR) == "Y");
				bool donor = (dataReader.GetString(indexDonor) == "Y");
				string addressLine1 = dataReader.GetString(indexAddressLine1);
				string addressLine2 = dataReader.GetString(indexAddressLine2);
				string addressCity = dataReader.GetString(indexAddressCity);
				string addressState = dataReader.GetString(indexAddressState);
				string addressZip = dataReader.GetString(indexAddressZip);
				BasicAddress address = new BasicAddress(addressLine1, addressLine2, addressCity, addressState, addressZip);
				patient = new Patient(lastName, firstName, middleInitial[0], gender[0], ssn, birthDate, address, dnr, donor);
				entryDate = dataReader.GetDateTime(indexEntryDate);
				exitDate = dataReader.GetDateTime(indexExitDate);
				diagnosis = new Diagnosis(dataReader.GetString(indexDiagnosis));
				notes = dataReader.GetString(indexNotes);
				insurer = dataReader.GetString(indexInsurer);
			}
			dataReader.Close();
			string roomNumber = Room.getRoomNumber(ssn, entryDate);
			currentRoom = new Room(entryDate, roomNumber);
			string symptoms = Diagnosis.getSymptoms(patient.getSSN(), entryDate);
			foreach (string symptom in symptoms.Split(','))
			{
				if (symptom != "")
				{
					diagnosis.addSymptom(symptom);
				}
			}
		}

		public static DataTable getMedicalRecords()
		{
			DataTable records = new DataTable();
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Get_Medical_Records_Top";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			SqlDataReader reader = command.ExecuteReader();
			records.Load(reader);
			return records;
		}

		public static DataTable searchMedicalRecords(string input)
		{
			DataTable records = new DataTable();
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Search_Medical_Records";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@input", input));
			SqlDataReader reader = command.ExecuteReader();
			records.Load(reader);
			return records;
		}
 
        public static DataTable searchAdvanceMedicalRecords(string firstName, string lastName, string patientSSN, string roomNum)
        {
            DataTable records = new DataTable();
            SqlConnection connection = Session.getCurrentSession().getConnection();
            string queryString = "SearchAdvance_Medical_Records";
            SqlCommand command = new SqlCommand(queryString, connection);
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@firstName", firstName));
            command.Parameters.Add(new SqlParameter("@lastName", lastName));
            command.Parameters.Add(new SqlParameter("@patientSSN", patientSSN));
            command.Parameters.Add(new SqlParameter("@roomNum", roomNum));
            SqlDataReader reader = command.ExecuteReader();
            records.Load(reader);
            return records;
        }

        public Room getRoom()
		{
			return currentRoom;
		}

		public string getFirstName()
		{
			return patient.getFirstName();
		}

		public string getLastName()
		{
			return patient.getLastName();
		}

		public char getMiddleInitial()
		{
			return patient.getMiddleInitial();
		}

		public DateTime getEntryDate()
		{
			return entryDate;
		}

		public DateTime getExitDate()
		{
			return exitDate;
		}

		public Patient getPatient()
		{
			return patient;
		}

		public string getNotes()
		{
			return notes;
		}

		public string getInsurer()
		{
			return insurer;
		}

		public Diagnosis getDiagnosis()
		{
			return diagnosis;
		}

		public void addSupply(InventoryItem item)
		{
			suppliesUsed.Add(item);
			// update the database here
		}

		public void removeSupply(InventoryItem item)
		{

		}

		public void checkIn(DateTime date)
		{
			entryDate = date;

		}

		public void checkOut(DateTime date)
		{

		}

		public void generateBill()
		{

		}

		public void addNote(string note)
		{

		}

		public void diagnose(Diagnosis theDiagnosis)
		{
			diagnosis = theDiagnosis;

			//connection.Open();
			//SqlCommand command = new SqlCommand(commandString, connection);
			//command.ExecuteNonQuery();
			//command.Dispose();
			//connection.Close();
		}

		public void addRoom(Room theRoom)
		{

		}

		public void removeRoom(Room theRoom)
		{

		}
	}

	public class InventoryItem
	{
		string stockID;
		int quantity;
		string description;
		int size;
		double cost;
		public static Exception inadaquateQuantity = new Exception("The requested withdrawal exceeds the total quantity.");
		public static Exception invalidInput = new Exception("The input format was incorrect.");
		public static Exception itemDoesNotExist = new Exception("The specified item id does not exist in the database.");
		public static Exception patientDoesNotExist = new Exception("The specified patient is not present in the database.");

		public double getTotalCost()
		{
			return 0;
		}

        public static DataTable  getInventory()
        {
            DataTable inventory = new DataTable();
            SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Get_Items_Top";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			SqlDataReader reader = command.ExecuteReader();
			inventory.Load(reader);
            return inventory;
        }

		public static DataTable getInventoryHistory()
		{
			DataTable inventory = new DataTable();
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Retrieve_Inventory_History";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			SqlDataReader reader = command.ExecuteReader();
			inventory.Load(reader);
			return inventory;
		}

		public static DataTable searchInventory(string id, string description, string size)
        {
            DataTable inventory = new DataTable();
            SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Search_Items";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@stockID", id));
			command.Parameters.Add(new SqlParameter("@description", description));
			command.Parameters.Add(new SqlParameter("@size", size));
			SqlDataReader reader = command.ExecuteReader();
            inventory.Load(reader);
            return inventory;
        }

		public static bool addInventory(string description, string stockID, string size, string quantity, string price)
		{
			SqlConnection connection = Session.getCurrentSession().getConnection();
            
            string queryString = "Import_Item";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@stockID", stockID));
			command.Parameters.Add(new SqlParameter("@description", description));
			command.Parameters.Add(new SqlParameter("@size", size));
			try
			{
				int numQuantity = Int32.Parse(quantity);
				int numPrice = Int32.Parse(price);
				command.Parameters.Add(new SqlParameter("@cost", numPrice));
				command.Parameters.Add(new SqlParameter("@quantity", numQuantity));
			}
			catch (Exception) { throw invalidInput; }
			try
			{
				command.ExecuteNonQuery();
				return true;
			}
			catch (Exception) { return false; }
		}

		public static bool addInventory(string stockID, string numQuantity)
		{
			string queryString = "Add_To_Existing_Inventory";
			SqlConnection connection = Session.getCurrentSession().getConnection();
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@stockID", stockID));
			command.Parameters.Add(new SqlParameter("@quantity", numQuantity));
			try
			{
				command.ExecuteNonQuery();
				return true;
			}
			catch (Exception) { return false; }
		}

		public static bool itemExists(string id)
		{
			SqlConnection connection = Session.getCurrentSession().getConnection();
			bool alreadyExists = false;
			string queryString = "Retrieve_Item";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@stockID", id));
			SqlDataReader dataReader = command.ExecuteReader();
			while (dataReader.Read())
			{
				alreadyExists = true;
			}
			dataReader.Close();
			return alreadyExists;
		}

		public static decimal getItemQuantity(string id)
		{
			decimal retrievedQuantity = 0;
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Retrieve_Item_Quantity";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@stockID", id));
			SqlDataReader dataReader = command.ExecuteReader();
			while (dataReader.Read())
			{
				int index = dataReader.GetOrdinal("Quantity");
				retrievedQuantity = dataReader.GetDecimal(index);
			}
			dataReader.Close();
			return retrievedQuantity;
		}

		public static bool withdrawItem(string id, string quantity, string ssn, string date)
		{
			if (!itemExists(id)) throw itemDoesNotExist;
			if (!Patient.checkPatient(ssn)) throw patientDoesNotExist;
			SqlConnection connection = Session.getCurrentSession().getConnection();
			decimal numQuantity = 0;
			try
			{
				numQuantity = Decimal.Parse(quantity);
			}
			catch (Exception) { throw invalidInput; }
			if (getItemQuantity(id) < numQuantity) throw inadaquateQuantity;
			string queryString = "Withdraw_Inventory";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@stockID", id));
			string username = Session.getCurrentSession().getCurrentUser().getUsername();
			command.Parameters.Add(new SqlParameter("@username", username));
			command.Parameters.Add(new SqlParameter("@ssn", ssn));
			DateTime entryDate = Patient.getEntryDate(ssn);
			command.Parameters.Add(new SqlParameter("@entryDate", entryDate));
			command.Parameters.Add(new SqlParameter("@quantity", quantity));
			DateTime dtEntryDateTime = DateTime.ParseExact(date, "MM/dd/yyyy hh:mm:ss", null);
			command.Parameters.Add(new SqlParameter("@date", date));
			try
			{
				command.ExecuteNonQuery();
				return true;
			}
			catch (Exception) { return false; }
		}

	}

	public class Room
	{
		string roomNumber;
		double hourlyRate;
		DateTime checkedIn;
		DateTime checkedOut;
		bool occupancyStatus;

		public Room(DateTime date, string roomNum)
		{
			roomNumber = roomNum;
			hourlyRate = getHourlyRate(roomNum);
			checkIn(date);
			occupancyStatus = true;
		}

		public void checkIn(DateTime date)
		{
			checkedIn = date;
		}

		public void checkOut(DateTime date)
		{
			checkedOut = date;
			occupancyStatus = false;
		}

		public void viewPossibleCheckOut(DateTime date)
		{
			checkedOut = date;
		}

		public void adjustRate(double newRate)
		{
			hourlyRate = newRate;
		}

		public double calculateCost(Patient thePatient)
		{
			return hourlyRate * (checkedOut.Subtract(checkedIn)).TotalHours;
		}

		public double getHourlyRate()
		{
			return hourlyRate;
		}

		public static double getHourlyRate(string roomNumber)
		{
			decimal retrievedQuantity = 0;
			SqlConnection connection = Session.getCurrentSession().getConnection();
			string queryString = "Retrieve_Hourly_Rate";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@roomNumber", roomNumber));
			SqlDataReader dataReaderRate = command.ExecuteReader();
			while (dataReaderRate.Read())
			{
				int index = dataReaderRate.GetOrdinal("Hourly_Rate");
				retrievedQuantity = dataReaderRate.GetDecimal(index);
			}
			dataReaderRate.Close();
			return (double)retrievedQuantity;

		}

		public static string getRoomNumber(string ssn, DateTime entryDate)
		{
			string connectionString = Properties.Settings1.Default.CONNECTIONSTRING;
			SqlConnection connection = new SqlConnection(connectionString);
			connection.Open();
			string queryString = "Retrieve_Room_Number";
			SqlCommand command = new SqlCommand(queryString, connection);
			command.CommandType = System.Data.CommandType.StoredProcedure;
			command.Parameters.Add(new SqlParameter("@ssn", ssn));
			command.Parameters.Add(new SqlParameter("@entryDate", entryDate));
			SqlDataReader dataReader = command.ExecuteReader();
			int index = dataReader.GetOrdinal("Room_Number");
			string roomNumber = "";
			while (dataReader.Read())
			{
				roomNumber = dataReader.GetString(index);
			}
			dataReader.Close();
			command.Dispose();
			return roomNumber;
		}

		public string getRoomNumber()
		{
			return roomNumber;
		}

		public DateTime getEntryDate()
		{
			return checkedIn;
		}

		public DateTime getExitDate()
		{
			return checkedOut;
		}

		public bool isOccupied()
		{
			return occupancyStatus;
		}

		public double getDuration()
		{
			return (checkedOut.Subtract(checkedIn)).TotalHours;
		}
	}

	public class ImportData
	{
		public static int progress = 0;

		public static int getProgress()
		{
			return progress;
		}

		public static void import(string filePath, ImportType type, Session session)
		{
			Thread thread;
			long fileSize = new System.IO.FileInfo(filePath).Length;
			System.IO.StreamReader file = new System.IO.StreamReader(@filePath);
			switch (type)
			{
				case ImportType.INVENTORY:
					thread = new Thread(() => importInventory(file, session.getConnection(), fileSize));
					thread.Start();
					break;
				case ImportType.MEDICAL:
					thread = new Thread(() => importMedical(file, session.getConnection(), fileSize));
					thread.Start();
					break;
				case ImportType.ROOM:
					thread = new Thread(() => importRoom(file, session.getConnection(), fileSize));
					thread.Start();
					break;
				case ImportType.USERS:
					thread = new Thread(() => importUser(file, session.getConnection(), fileSize));
					thread.Start();
					break;
			}
		}

		private static void importInventory(System.IO.StreamReader file, SqlConnection connection, long fileSize)
		{
			int bytesRead = 0;
			string line;
			int quantity = 0;
			bool hasQuantity = false;
			while ((line = file.ReadLine()) != null) // all casts are just integer enumerations to make it more readable
			{
				bytesRead += line.Length;
                string id = line.Substring((int)DataStart.STOCKID, (int)DataLength.STOCKID);
				try
				{
					quantity = Int32.Parse(line.Substring((int)DataStart.QUANTITY, (int)DataLength.QUANTITY));
					hasQuantity = true;
				}
				catch (Exception)
				{
					hasQuantity = false;
				}
                string description = line.Substring((int)DataStart.DESCRIPTION, (int)DataLength.DESCRIPTION);
                string size = line.Substring((int)DataStart.SIZE, (int)DataLength.SIZE);
                int cost = Int32.Parse(line.Substring((int)DataStart.COST, (int)DataLength.COST));

				string queryString = "Import_Inventory";
				SqlCommand command = new SqlCommand(queryString, connection);
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.Parameters.Add(new SqlParameter("@stockID", id));
				command.Parameters.Add(new SqlParameter("@quantity", quantity));
				command.Parameters.Add(new SqlParameter("@description", description));
				command.Parameters.Add(new SqlParameter("@size", size));
				command.Parameters.Add(new SqlParameter("@cost", cost));
				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception) { } // This is for the duplictes that occur
                command.Dispose();
				double updatedProgress = (bytesRead * 100) / fileSize;
				if (updatedProgress < 100) progress = (int)updatedProgress; // ok because it will always be less than a hundred
			}
			progress = 100;
			file.Close();
		}

		private static void importMedical(System.IO.StreamReader file, SqlConnection connection, long fileSize)
		{
			int bytesRead = 0;
			string line;
			while ((line = file.ReadLine()) != null)
			{
				bytesRead += line.Length;
				string lastName = line.Substring((int)DataStart.LASTNAME, (int)DataLength.LASTNAME);
				lastName = lastName.Trim();
				string firstName = line.Substring((int)DataStart.FIRSTNAME, (int)DataLength.FIRSTNAME);
				firstName = firstName.Trim();
				string middleInitial = line.Substring((int)DataStart.MIDDLEINITIAL, (int)DataLength.MIDDLEINITIAL);
				string gender = line.Substring((int)DataStart.GENDER, (int)DataLength.GENDER);
				string ssn = line.Substring((int)DataStart.SSN, (int)DataLength.SSN);
				string birthDate = line.Substring((int)DataStart.BIRTHDATE, (int)DataLength.BIRTHDATE);
				string entryDateTime = line.Substring((int)DataStart.ENTRYDATETIME, (int)DataLength.ENTRYDATETIME);
				DateTime dtEntryDateTime = DateTime.ParseExact(entryDateTime, "MMddyyyyHHmm", null);
				string exitDateTime = line.Substring((int)DataStart.EXITDATETIME, (int)DataLength.EXITDATETIME);
				DateTime dtExitDateTime = DateTime.ParseExact(exitDateTime, "MMddyyyyHHmm", null);
				string attendingPhys = line.Substring((int)DataStart.ATTENDINGPHY, (int)DataLength.ATTENDINGPHY);
				string roomNo = line.Substring((int)DataStart.ROOMNO, (int)DataLength.ROOMNO);
				string symptom1 = line.Substring((int)DataStart.SYMPTOM1, (int)DataLength.SYMPTOM1);
				symptom1 = symptom1.Trim();
				string symptom2 = line.Substring((int)DataStart.SYMPTOM2, (int)DataLength.SYMPTOM2);
				symptom2 = symptom2.Trim();
				string symptom3 = line.Substring((int)DataStart.SYMPTOM3, (int)DataLength.SYMPTOM3);
				symptom3 = symptom3.Trim();
				string symptom4 = line.Substring((int)DataStart.SYMPTOM4, (int)DataLength.SYMPTOM4);
				symptom4 = symptom4.Trim();
				string symptom5 = line.Substring((int)DataStart.SYMPTOM5, (int)DataLength.SYMPTOM5);
				symptom5 = symptom5.Trim();
				string symptom6 = line.Substring((int)DataStart.SYMPTOM6, (int)DataLength.SYMPTOM6);
				symptom6 = symptom6.Trim();
				string diagnosis = line.Substring((int)DataStart.DIAGNOSIS, (int)DataLength.DIAGNOSIS);
				diagnosis = diagnosis.Trim();
				string notes = line.Substring((int)DataStart.NOTES, (int)DataLength.NOTES);
				string insurer = line.Substring((int)DataStart.INSURER, (int)DataLength.INSURER);
				string addressLine1 = line.Substring((int)DataStart.ADDRESSLINE1, (int)DataLength.ADDRESSLINE1);
				string addressLine2 = line.Substring((int)DataStart.ADDRESSLINE2, (int)DataLength.ADDRESSLINE2);
				string addressCity = line.Substring((int)DataStart.ADDRESSCITY, (int)DataLength.ADDRESSCITY);
				addressCity = addressCity.Trim();
				string addressState = line.Substring((int)DataStart.ADDRESSSTATE, (int)DataLength.ADDRESSSTATE);
				string addressZip = line.Substring((int)DataStart.ADDRESSZIP, (int)DataLength.ADDRESSZIP);
				string dnrStatus = line.Substring((int)DataStart.DNRSTATUS, (int)DataLength.DNRSTATUS);
				string organDonor = line.Substring((int)DataStart.ORGANDONOR, (int)DataLength.ORGANDONOR);

				string queryString = "Import_Patient";
				SqlCommand command = new SqlCommand(queryString, connection);
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.Parameters.Add(new SqlParameter("@lastName", lastName));
				command.Parameters.Add(new SqlParameter("@firstName", firstName));
				command.Parameters.Add(new SqlParameter("@middleInitial", middleInitial));
				command.Parameters.Add(new SqlParameter("@gender", gender));
				command.Parameters.Add(new SqlParameter("@ssn", ssn));
				command.Parameters.Add(new SqlParameter("@birthDate", birthDate));
				command.Parameters.Add(new SqlParameter("@addressLine1", addressLine1));
				command.Parameters.Add(new SqlParameter("@addressLine2", addressLine2));
				command.Parameters.Add(new SqlParameter("@addressCity", addressCity));
				command.Parameters.Add(new SqlParameter("@addressState", addressState));
				command.Parameters.Add(new SqlParameter("@addressZip", addressZip));
				command.Parameters.Add(new SqlParameter("@dnrStatus", dnrStatus));
				command.Parameters.Add(new SqlParameter("@organDonor", organDonor));
				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception) { } // This is for the duplictes that occur

				queryString = "Import_Medical_Record";
				command = new SqlCommand(queryString, connection);
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.Parameters.Add(new SqlParameter("@ssn", ssn));
				command.Parameters.Add(new SqlParameter("@entryDateTime", dtEntryDateTime));
				command.Parameters.Add(new SqlParameter("@exitDateTime", dtExitDateTime));
				command.Parameters.Add(new SqlParameter("@diagnosis", diagnosis));
				command.Parameters.Add(new SqlParameter("@insurer", insurer));
				command.Parameters.Add(new SqlParameter("@notes", notes));
				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception) { } // This is for the duplictes that occur

				string[] symptomList = { symptom1, symptom2, symptom3, symptom4, symptom5, symptom6 };
				foreach (string symptom in symptomList)
				{
					bool alreadyExists = false;
					queryString = "Retrieve_Symptom";
					command = new SqlCommand(queryString, connection);
					command.CommandType = System.Data.CommandType.StoredProcedure;
					command.Parameters.Add(new SqlParameter("@symptomName", symptom));
					SqlDataReader dataReader = command.ExecuteReader();
					while (dataReader.Read())
					{
						alreadyExists = true;
					}
					dataReader.Close();
					command.Dispose();

					if (!alreadyExists)
					{
						queryString = "Import_Symptom";
						command = new SqlCommand(queryString, connection);
						command.CommandType = System.Data.CommandType.StoredProcedure;
						command.Parameters.Add(new SqlParameter("@symptomName", symptom));
						try
						{
							command.ExecuteNonQuery();
						}
						catch (Exception) { } // This is for the duplictes that occur
					}

					queryString = "Import_Show_Signs";
					command = new SqlCommand(queryString, connection);
					command.CommandType = System.Data.CommandType.StoredProcedure;
					command.Parameters.Add(new SqlParameter("@symptomName", symptom));
					command.Parameters.Add(new SqlParameter("@ssn", ssn));
					command.Parameters.Add(new SqlParameter("@entryDate", dtEntryDateTime));
					try
					{
						command.ExecuteNonQuery();
					}
					catch (Exception) { } // This is for the duplictes that occur
				}

				queryString = "Import_Stayed_In";
				command = new SqlCommand(queryString, connection);
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.Parameters.Add(new SqlParameter("@roomNumber", roomNo));
				command.Parameters.Add(new SqlParameter("@ssn", ssn));
				command.Parameters.Add(new SqlParameter("@entryDate", dtEntryDateTime));
				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception) { } // This is for the duplictes that occur

				// TODO: !!!!!!!!!!!!!!!!!STILL NEED ATTENDING PHYSICIAN!!!!!!!!!!!!!!!!!!!!!!!

				command.Dispose();
				double updatedProgress = (bytesRead * 100) / fileSize;
				if (updatedProgress < 100) progress = (int)updatedProgress; // ok because it will always be less than a hundred
			}
			progress = 100;
			file.Close();
		}

		private static void importRoom(System.IO.StreamReader file, SqlConnection connection, long fileSize)
		{
			int bytesRead = 0;
			string line;
			while ((line = file.ReadLine()) != null)
			{
				bytesRead += line.Length;
				string roomNumber = line.Substring((int)DataStart.ROOMNUMBER, (int)DataLength.ROOMNUMBER);
				decimal hourlyRate = Decimal.Parse(line.Substring((int)DataStart.HOURLYRATE, (int)DataLength.HOURLYRATE));
				hourlyRate /= 100;
				string effectiveDate = line.Substring((int)DataStart.EFFECTIVEDATE, (int)DataLength.EFFECTIVEDATE);
				string queryString = "Import_Room";
				SqlCommand command = new SqlCommand(queryString, connection);
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.Parameters.Add(new SqlParameter("@roomNumber", roomNumber));
				command.Parameters.Add(new SqlParameter("@hourlyRate", hourlyRate));
				command.Parameters.Add(new SqlParameter("@effectiveDate", effectiveDate));
				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception) { } // This is for the duplictes that occur

				command.Dispose();
				double updatedProgress = (bytesRead * 100) / fileSize;
				if (updatedProgress < 100) progress = (int)updatedProgress; // ok because it will always be less than a hundred
			}
			progress = 100;
			file.Close();
		}

		private static void importUser(System.IO.StreamReader file, SqlConnection connection, long fileSize)
		{
			int bytesRead = 0;
			string line;
			while ((line = file.ReadLine()) != null)
			{
				bytesRead += line.Length;
				string username = line.Substring((int)DataStart.USERNAME, (int)DataLength.USERNAME);
				username = username.Trim();
				string password = line.Substring((int)DataStart.PASSWORD, (int)DataLength.PASSWORD);
				password = password.Trim();
				string hashedPassword = PasswordHasher.hashPassword(password);
				string userType = line.Substring((int)DataStart.USERTYPE, (int)DataLength.USERTYPE);
				string queryString = "Import_User";
				SqlCommand command = new SqlCommand(queryString, connection);
				command.CommandType = System.Data.CommandType.StoredProcedure;
				command.Parameters.Add(new SqlParameter("@username", username));
				command.Parameters.Add(new SqlParameter("@password", hashedPassword));
				command.Parameters.Add(new SqlParameter("@userType", userType));
				command.Parameters.Add(new SqlParameter("@failedLogins", '0'));
				try
				{
					command.ExecuteNonQuery();
				}
				catch (Exception) { } // This is for the duplictes that occur

				command.Dispose();
				double updatedProgress = (bytesRead * 100) / fileSize;
				if (updatedProgress < 100) progress = (int)updatedProgress; // ok because it will always be less than a hundred
			}
			progress = 100;
			file.Close();
		}
	}
}