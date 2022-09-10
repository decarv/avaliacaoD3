using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;

Console.WriteLine("Hello, World!");

namespace Application
{
    public class Log
    {
        public string Filepath { get; }
        public Log(string filepath)
        {
            Filepath = filepath;
            if (!File.Exists(Filepath))
            {
                File.Create(Filepath);
            }
        }
        public void LogUserLogin(Guid guid, string name, DateTime datetime)
        {
            string[] lines =
            {
                guid.ToString(),
                name,
                datetime.ToString(),
            };
            File.WriteAllLines(Filepath, lines);
        }
    }

    public class Server
    {
        private readonly string connString = "";
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
            string query = "SELECT email FROM users WHERE email='@email'";
            SqlDataReader rdr;
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@email", email);
                rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (email == rdr["email"])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public Dictionary<string, string> RequestAuthenticate(string email, ConsoleKeyInfo password)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            if (conn != null && conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string query = "SELECT guid,email,password,name FROM users WHERE email='@email'";
            SqlDataReader rdr;
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@email", email);
                rdr = cmd.ExecuteReader();
                rdr.Read();
                if (password.KeyChar.ToString() != rdr["password"])                                     // TODO: properly compare the password stored in db
                {
                    result["response"] = "ERROR";
                }
                else
                {
                    result["name"] = rdr["name"];
                    result["guid"] = rdr["guid"];
                    result["password"] = rdr["password"];
                    result["email"] = rdr["email"];
                    result["response"] = "OK";
                }
            }
            return result;
        }
    }

    public class User
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Guid GUID { get; set; }

        public User(string guid, string name, string password)
        {
            Guid GUID = new Guid(guid);
            Name = name;
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

        private bool userLoggedIn;
        private MenuOption menuOption;
        private Log log;
        private Server server;
        private User user;
        public Application()
        {
            userLoggedIn = false;
            menuOption = MenuOption.Welcome;
            log = new Log("./log.txt");
            server = new Server();
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
        public void PrintWelcomeMenu() {
            int intResponse;
            while (true)
            {
                Console.Write("Digite [1] para ACESSAR e [2] para CANCELAR: ");
                string strResponse = Console.ReadLine();
                bool validResponse = Int32.TryParse(strResponse, out intResponse);
                if (validResponse == false || intResponse != 2 || intResponse != 1)
                {
                    Console.WriteLine("Opção não disponível!");
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
                }
            }
        }

        private void PrintUserMenu()
        {
            int intResponse;
            while (true)
            {
                Console.Write("Digite [1] para DESLOGAR e [2] para ENCERRAR: ");
                string strResponse = Console.ReadLine();
                bool validResponse = Int32.TryParse(strResponse, out intResponse);
                if (validResponse == false || intResponse != 1 || intResponse != 2)
                {
                    Console.WriteLine("Opção não disponível...");
                    return;
                }
                else if (intResponse == 2)
                {
                    menuOption = MenuOption.ExitApplication;
                    return;
                }
                else
                {
                    MenuOption menuOption = MenuOption.Login;
                    return;
                }
            }
        }
        private void PrintLoginMenu()
        {
            while (true)
            {
                Console.Write("Digite seu e-mail: ");
                string email = Console.ReadLine();
                if (!ExistsEmail(email))
                {
                    Console.WriteLine("ERRO: O e-mail digitado não foi encontrado nos servidores.");
                    continue;
                }
                Console.Write("Digite sua senha: ");
                ConsoleKeyInfo password = Console.ReadKey();
                Dictionary<string,string> auth = Authenticate(email, password); // TODO
                if (auth["response"] == "ERROR")
                {
                    Console.WriteLine("ERRO: A senha digitada é incorreta. Refaça toda a operação.");
                    continue;
                }
                else
                {
                    Console.WriteLine("Login bem sucedido!");
                    user = new User(auth["guid"], auth["name"], auth["password"]);
                    DateTime localDate = DateTime.Now;
                    log.LogUserLogin(user.GUID, user.Name, localDate);
                    userLoggedIn = true;
                    menuOption = MenuOption.User;
                    return;
                }
            }
        }

        public bool ExistsEmail(string email)
        {
            return server.RequestExistsEmail(email);
        }

        public Dictionary<string,string> Authenticate(string email, ConsoleKeyInfo password)
        {
            return server.RequestAuthenticate(email, password);
        }
    }

    public class Program
    {
        public void Main()
        {
            Application app = new Application();
            app.Run();
        }
    }
}
