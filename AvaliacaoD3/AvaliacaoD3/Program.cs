using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
using System.Collections.Specialized;

namespace Application
{
    public class Log
    {
        public enum LogMessage
        {
            Login = 0,
            Logout = 1
        }

        private Dictionary<LogMessage, string> stringLogMessage = new Dictionary<LogMessage, string> 
        {
            {LogMessage.Login,  "LOGIN"},
            {LogMessage.Logout, "LOGOUT"}
        };

        private string MessageToString(LogMessage lm)
        {
            return stringLogMessage[lm];
        }

        public string Filepath { get; }
        public Log(string filepath)
        {
            Filepath = filepath;
            if (!File.Exists(Filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine("DATE,EMAIL,UID,LOG");
                }
            }
        }

        public void Message(LogMessage message, Guid uid, string email) 
        {
            DateTime datetime = DateTime.Now;
            string composedMessage = $"{datetime},{email},{uid},{MessageToString(message)}";
            using (StreamWriter sw = File.AppendText(Filepath))
            {
                sw.WriteLine(composedMessage);
            }
        }
    }

    public class Server
    {
        private readonly string connString = $"Server=labsoft.pcs.usp.br; Initial Catalog=db_10; User id=usuario_10; pwd=;";
        SqlConnection conn;

        public Server()
        {
            conn = new SqlConnection(connString);
            conn.Open();
        }

        ~Server()
        {
            conn.Close();
        }

        public bool RequestExistsEmail(string email)
        {
            if (conn != null && conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string query = "SELECT * FROM users WHERE email = @email";
            using (SqlCommand cmd = new(query, conn))
            {
                cmd.Parameters.AddWithValue("@email", email);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && email.Equals(reader["email"]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Dictionary<string, string?> RequestAuthenticate(string email, string password)
        {
            Dictionary<string, string?>  responseDictionary = new();

            if (conn != null && conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string query = "SELECT * FROM users WHERE email = @email";
            using (SqlCommand cmd = new(query, conn))
            {
                cmd.Parameters.AddWithValue("@email", email);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read() && password.Equals(reader["password"].ToString()))                                     // TODO: properly compare the password stored in db
                    {
                        responseDictionary.Add("uid", reader["uid"].ToString());
                        responseDictionary.Add("password", reader["password"].ToString());
                        responseDictionary.Add("email", reader["email"].ToString());
                        responseDictionary.Add("responseStatus", "OK");
                    }
                    else
                    {
                        responseDictionary.Add("responseStatus", "ERROR");
                    }
                }
            }
            return responseDictionary;
        }
    }

    public class User
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public Guid GUID { get; set; }

        public User(string uid, string email, string password)
        {
                GUID = new(uid);
                Email = email;
                Password = password;
        }
    }

    public class Application
    {
        private enum MenuOption
        {
            Welcome = 0,
            Login = 1,
            User = 2,
            ExitApplication = 3
        };

        private MenuOption menuOption;
        private Log log;
        private Server? server;
        private User? user;
        public Application()
        {
            menuOption = MenuOption.Welcome;
            log = new("logfile.txt");
            user = null;
            server = null;
        }

        public void Run()
        {
            while (true)
            {
                switch (menuOption)
                {
                    case MenuOption.Welcome:
                        PrintWelcomeMenu();
                        break;
                    case MenuOption.User:
                        PrintUserMenu();
                        break;
                    case MenuOption.Login:
                        PrintLoginMenu();
                        break;
                    case MenuOption.ExitApplication:
                        Console.WriteLine("Até mais!");
                        return;
                }
            }
        }

        public void PrintWelcomeMenu()
        {
            Console.WriteLine("\n\t##### Menu da Aplicação #####");
            int intResponse = -1;
            while (true)
            {
                Console.Write("Digite [1] para ACESSAR e [2] para CANCELAR: ");
                string? strResponse = Console.ReadLine();
                bool validResponse = false;
                if (strResponse != null)
                {
                    validResponse = int.TryParse(strResponse, out intResponse);
                }

                if (!validResponse)
                {
                    Console.WriteLine("Opção inválida!");
                }
                else if (intResponse != 2 && intResponse != 1)
                {
                    Console.WriteLine("Opção não disponível.");
                    return;
                }
                else if (intResponse == 2)
                {
                    menuOption = MenuOption.ExitApplication;
                    return;
                }
                else
                {
                    menuOption = MenuOption.Login;
                    ConnectToServer();
                    return;
                }
            }
        }

        private void PrintUserMenu()
        {
            int intResponse = -1;
            while (true)
            {
                Console.Write("Digite [1] para DESLOGAR e [2] para ENCERRAR: ");
                string? strResponse = Console.ReadLine();

                bool validResponse = false;
                if (strResponse != null)
                {
                    validResponse = int.TryParse(strResponse, out intResponse);
                }

                if (validResponse == false && intResponse != 1 && intResponse != 2)
                {
                    Console.WriteLine("Opção não disponível.");
                    return;
                }
                else if (intResponse == 1)
                {
                    UserLogout();
                    menuOption = MenuOption.Login;
                    return;
                }
                else
                {
                    UserLogout();
                    menuOption = MenuOption.ExitApplication;
                    return;
                }
            }
        }

        private void UserLogout()
        {
            if (user != null)
            {
                log.Message(Log.LogMessage.Logout, user.GUID, user.Email);
                user = null;
                Console.WriteLine("Logout bem sucedido!");
            }
        }
        
        private void ConnectToServer()
        {
            server = new();
        }

        private void PrintLoginMenu()
        {
            Console.WriteLine("\n\t##### Menu de Login ##### ");
            while (true)
            {
                Console.Write("Digite seu e-mail: ");
                string? email = Console.ReadLine();
                if (email == null || !ExistsEmail(email))
                {
                    Console.WriteLine("ERRO: O e-mail digitado não foi encontrado nos servidores.\n");
                    continue;
                }
                Console.Write("Digite sua senha: ");
                string? password = Console.ReadLine();
                Dictionary<string, string?>? auth = Authenticate(email, password); // TODO
                if (auth == null)
                {
                    Console.WriteLine("ERRO: O servidor foi incapaz de autenticar a senha.");
                    return;
                }
                if (auth["responseStatus"] == "ERROR")
                {
                    Console.WriteLine("ERRO: A senha digitada é incorreta. Refaça toda a operação.");
                    continue;
                }
                else
                {
                    Console.WriteLine("Login bem sucedido!");
                    user = new(auth["uid"], auth["email"], auth["password"]);
                    log.Message(Log.LogMessage.Login, user.GUID, user.Email);
                    menuOption = MenuOption.User;
                    return;
                }
            }
        }

        public bool ExistsEmail(string email)
        {
            return server.RequestExistsEmail(email);
        }

        public Dictionary<string, string?> Authenticate(string email, string password)
        {
            return server.RequestAuthenticate(email, password);
        }
    }

    public class Program
    {
        public static void Main()
        {
            Application app = new();
            app.Run();
        }
    }
}

