using System.Data.SqlClient;
using System.Threading.Tasks;

namespace jobsapp.Helperdb
{
    public static class DbHelper
    {
        public static async Task<bool> InsertUser(string firstName, string lastName, string email, string password, string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string sql = "INSERT INTO jobapp.applicants ( first_name,last_name,email, password) VALUES ( @firstName,@lastName, @Email, @Password)";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@firstName", firstName);
                    cmd.Parameters.AddWithValue("@lastName", lastName);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    
                    //rows would increment after user is added to db
                    int rows = await cmd.ExecuteNonQueryAsync();
                    return rows > 0;
                }
            }
        }

        public static async Task<string?> GetUserByEmail(string email, string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string sql = "SELECT Password FROM jobapp.applicants WHERE Email = @Email";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader["Password"].ToString();
                        }
                        else
                        {   
                            //return null if user email is not found
                            return null;
                        }
                    }
                }
            }
        }
    }
}