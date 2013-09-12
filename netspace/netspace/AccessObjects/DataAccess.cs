using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace netspace.AccessObjects
{
    public partial class DataAccess : IDisposable
    {
        #region [ Fields ]
        private SqlConnection Connection;
        private SqlTransaction CurrentTransaction;
        private SqlCommand Command;
        public String DatabaseName { get; set; }
        #endregion

        #region [ Constructors ]
        public DataAccess()
        {

        }

        public DataAccess(bool safe_connect, String Connection_String_Name)
        {
            string connection_string = ConfigurationManager.ConnectionStrings[Connection_String_Name].ConnectionString;
            this.CreateConnection(connection_string);
        }

        public DataAccess(string ConnectionString)
        {
            this.CreateConnection(ConnectionString);
            this.DatabaseName = this.Connection.Database;
        }
        #endregion

        #region [ Connection Management Methods ]
        /// <summary>
        /// Initializes the Sql Connection Object 
        /// </summary>
        /// <param name="connectionStringName">ADO.NET Connection String</param>
        public virtual void CreateConnection(string ConnectionString)
        {
            if ((this.CurrentTransaction == null))
            {
                Connection = new SqlConnection(ConnectionString);
            }

        }
        #endregion

        #region [ SQL Command Methods ]
        public virtual void AddParameter(string name, object value, ParameterDirection direction)
        {
            if ((this.Command == null))
            {
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            SqlParameter parameter = new SqlParameter(name, this.GetValue(value));
            parameter.Direction = direction;
            this.Command.Parameters.Add(parameter);
        }

        public virtual void AddParameter(string name, object value, ParameterDirection direction, String UserDefinedTableTypeName)
        {
            if ((this.Command == null))
            {
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            SqlParameter parameter = new SqlParameter(name, SqlDbType.Structured, 0);
            parameter.TypeName = UserDefinedTableTypeName;
            parameter.SqlDbType = SqlDbType.Structured;
            parameter.ParameterName = name;
            parameter.Value = value;
            parameter.Direction = direction;
            this.Command.Parameters.Add(parameter);
        }

        public virtual void AddParameter(System.Data.Common.DbParameter Parameter)
        {
            if ((this.Command == null))
            {
                return;
            }
            this.Command.Parameters.Add((SqlParameter)Parameter);
        }

        public virtual void CreateProcedureCommand(string storedProcedureName)
        {
            if ((Connection == null))
            {
                throw new ApplicationException("Call CreateConnection method before using the connection.");
            }
            this.Command = new SqlCommand(storedProcedureName, this.Connection);
            this.Command.CommandType = CommandType.StoredProcedure;
        }
        #endregion

        #region [ Projection Methods ]

        public virtual DataSet ExecuteDataSet()
        {
            if ((this.Command == null))
            {
                throw new ApplicationException("The command object is not defined.");
            }
            if ((Connection == null))
            {
                if (this.DatabaseName == null)
                {
                    throw new ApplicationException("Call CreateConnection method before using the connection. Database Name is also blank.");
                }
            }


            SqlDataAdapter da = null;
            DataSet ds;
            try
            {
                da = new SqlDataAdapter(Command);
                ds = new DataSet();
                da.Fill(ds);
            }
            catch (SqlException sqlException)
            {
                throw sqlException;
            }
            finally
            {
                if ((da != null))
                {
                    da.Dispose();
                }
            }
            return ds;
        }

        public virtual SqlDataReader ExecuteDataReader()
        {
            if ((this.Command == null))
            {
                throw new ApplicationException("The command object is not defined.");
            }
            if ((Connection == null))
            {
                throw new ApplicationException("Call CreateConnection method before using the connection.");
            }
            ConnectionState initialState = ConnectionState.Closed;
            SqlDataReader reader = null;
            try
            {
                this.QuietOpen(out initialState);
                reader = this.Command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (System.Exception)
            {
                throw;
            }
            return reader;
        }

        public virtual int ExecuteNonQuery()
        {
            if ((this.Command == null))
            {
                throw new ApplicationException("The command object is not defined.");
            }
            if ((Connection == null))
            {
                throw new ApplicationException("Call CreateConnection method before using the connection.");
            }

            ConnectionState initialState = ConnectionState.Closed;
            int value;

            try
            {
                this.QuietOpen(out initialState);

                value = this.Command.ExecuteNonQuery();
            }
            catch (System.Exception x)
            {
                throw x;
            }
            finally
            {
                this.QuietClose(initialState);
            }
            return value;
        }

        public virtual object ExecuteScalar()
        {
            if ((this.Command == null))
            {
                throw new ApplicationException("The command object is not defined.");
            }
            if ((Connection == null))
            {
                throw new ApplicationException("Call CreateConnection method before using the connection.");
            }
            ConnectionState initialState = ConnectionState.Closed;
            object value;

            try
            {
                this.QuietOpen(out initialState);
                value = this.Command.ExecuteScalar();
            }
            catch (System.Exception)
            {
                throw;
            }
            finally
            {
                this.QuietClose(initialState);
            }

            return value;
        }

        public virtual DataSet ExecuteSql(String Sql)
        {
            if ((Connection == null))
            {
                throw new ApplicationException("Call CreateConnection method before using the connection.");
            }
            this.Command = new SqlCommand(Sql, this.Connection);
            SqlDataAdapter da = null;
            DataSet ds;
            try
            {
                da = new SqlDataAdapter(this.Command);
                ds = new DataSet();
                da.Fill(ds);
            }
            catch (System.Data.SqlClient.SqlException sqlException)
            {
                throw sqlException;
            }
            catch (System.Exception) // Need ot get specific here 
            {
                throw;
            }
            finally
            {
                if ((da != null))
                {
                    da.Dispose();
                }
            }
            return ds;
        }

        public virtual XmlReader ExecuteXmlReader()
        {
            if ((this.Command == null))
            {
                throw new ApplicationException("The command object is not defined.");
            }
            if ((Connection == null))
            {
                throw new ApplicationException("Call CreateConnection method before using the connection.");
            }

            ConnectionState initialState = ConnectionState.Closed;
            XmlReader value;

            try
            {
                this.QuietOpen(out initialState);
                value = this.Command.ExecuteXmlReader();
            }
            catch (System.Exception)
            {
                throw;
            }
            finally
            {
                this.QuietClose(initialState);
            }

            return value;
        }
        #endregion

        #region [ Transaction Methods ]
        public virtual void BeginTransaction()
        {
            if ((CurrentTransaction != null))
            {
                throw new ApplicationException("There is already a pending transaction. Can not start another one.");
            }
            if ((Connection == null))
            {
                throw new ApplicationException("Call CreateConnection method before using the connection.");
            }
            ConnectionState initialState;
            try
            {
                this.QuietOpen(out initialState);
                CurrentTransaction = Connection.BeginTransaction();
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public virtual void RollbackTransaction()
        {
            if ((CurrentTransaction != null))
            {
                CurrentTransaction.Rollback();
                this.QuietClose(ConnectionState.Closed);
                CurrentTransaction.Dispose();
            }
            else
            {
                throw new ApplicationException("No transaction to Rollback.");
            }
        }

        public virtual void CommitTransaction()
        {
            if ((CurrentTransaction != null))
            {
                CurrentTransaction.Commit();
                this.QuietClose(ConnectionState.Closed);
                CurrentTransaction.Dispose();
            }
            else
            {
                throw new ApplicationException("No transaction to Commit.");
            }
        }
        #endregion

        #region [ Error Handle ]
        private void HandleError()
        {
        }
        #endregion

        public virtual void Dispose()
        {
            if ((this.Command != null))
            {
                this.Command.Dispose();
            }
            if ((CurrentTransaction != null))
            {
                CurrentTransaction.Rollback();
                CurrentTransaction.Dispose();
            }
            if ((Connection != null))
            {
                Connection.Dispose();
            }
        }

        #region [ Local Methods ]
        private void QuietOpen(out ConnectionState initialState)
        {
            initialState = ConnectionState.Open;
            if ((this.Connection.State == ConnectionState.Closed))
            {
                initialState = ConnectionState.Closed;
                this.Connection.Open();
            }
        }

        private void QuietClose(ConnectionState initialState)
        {
            if ((initialState == ConnectionState.Closed))
            {
                this.Connection.Close();
            }
        }

        private object GetValue(object value)
        {
            if ((value == null))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(byte))
                        && (((byte)(value)) == byte.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(char))
                        && (((char)(value)) == char.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(decimal))
                        && (((decimal)(value)) == decimal.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(double))
                        && (((double)(value)) == double.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(float))
                        && (((float)(value)) == float.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(int))
                        && (((int)(value)) == int.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(long))
                        && (((long)(value)) == long.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(short))
                        && (((short)(value)) == short.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(string))
                        && (((string)(value)) == string.Empty)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(System.DateTime))
                        && (((System.DateTime)(value)) == System.DateTime.MinValue)))
            {
                return DBNull.Value;
            }
            if (((value.GetType() == typeof(System.Guid))
                        && (((System.Guid)(value)) == System.Guid.Empty)))
            {
                return DBNull.Value;
            }
            return value;
        }
        #endregion
    }
}
