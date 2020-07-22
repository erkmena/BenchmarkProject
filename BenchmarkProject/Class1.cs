using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;

namespace BenchmarkProject
{
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
        {
            data = new byte[N];
            new Random(42).NextBytes(data);
        }

        [Benchmark]
        public byte[] Sha256() => sha256.ComputeHash(data);

        [Benchmark]
        public byte[] Md5() => md5.ComputeHash(data);
    }
    public class AdovsDapper
    {
        string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=NORTHWND;"
            + "Integrated Security=true";

        // Provide the query string with a parameter placeholder.
        string queryString =
            "SELECT ProductID, UnitPrice, ProductName from dbo.products "
                + "WHERE UnitPrice > @pricePoint "
                + "ORDER BY UnitPrice DESC;";

        string complexQueryString = "SELECT Categories.CategoryName, Products.ProductName, " +
"Sum(CONVERT(money, (\"Order Details\".UnitPrice* Quantity*(1-Discount)/100))*100) AS ProductSales " +
" FROM(Categories INNER JOIN Products ON Categories.CategoryID = Products.CategoryID)" +
    "INNER JOIN(Orders        INNER JOIN \"Order Details\" ON Orders.OrderID = \"Order Details\".OrderID)    ON Products.ProductID = \"Order Details\".ProductID " +
"WHERE (((Orders.ShippedDate) Between '19970101' And '19971231')) GROUP BY Categories.CategoryName, Products.ProductName";

        // Specify the parameter value.
        int paramValue = 5;

        public class Product
        {
            public int ProductId { get; set; }
            public double UnitPrice { get; set; }
            public string ProductName { get; set; }
        }
        public class ComplexResult
        {
            public string CategoryName { get; set; }
            public string ProductName { get; set; }
            public double ProductSales { get; set; }
        }

        [Benchmark]
        public List<Product> ADOSQLClientTest()
        {
            List<Product> productList = new List<Product>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@pricePoint", paramValue);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        Product product = new Product() { ProductId = Convert.ToInt32(reader[0]), UnitPrice = Convert.ToDouble(reader[1]), ProductName = reader[2].ToString() };
                        productList.Add(product);
                    }
                    reader.Close();
                }
                catch (Exception)
                {
                }
                return productList;
            }
        }
        [Benchmark]
        public List<Product> DapperTest()
        {
            List<Product> productList;
            var parameter = new { PricePoint = paramValue };
            using (var connection = new SqlConnection(connectionString))
            {
                productList = connection.Query<Product>(queryString, parameter).ToList();
            }

            return productList;
        }

        [Benchmark]
        public List<ComplexResult> ADOComplexResult()
        {
            List<ComplexResult> productList = new List<ComplexResult>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(complexQueryString, connection);
                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        ComplexResult product = new ComplexResult() { CategoryName = reader[0].ToString(), ProductName = reader[1].ToString(), ProductSales = Convert.ToDouble(reader[2]) };
                        productList.Add(product);
                    }
                    reader.Close();
                }
                catch (Exception)
                {
                }
                return productList;
            }
        }
        [Benchmark]
        public List<ComplexResult> DapperComplexResult()
        {
            List<ComplexResult> productList;
            using (var connection = new SqlConnection(connectionString))
            {
                productList = connection.Query<ComplexResult>(complexQueryString).ToList();
            }

            return productList;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<AdovsDapper>();
        }
    }
}
