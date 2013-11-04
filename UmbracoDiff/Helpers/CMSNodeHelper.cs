using System;
using System.Data.SqlClient;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using UmbracoDiff.Entities;
using umbraco.cms.businesslogic.web;
using umbraco.DataLayer;

namespace UmbracoDiff.Helpers
{
    /// <summary>
    /// The bulk of this code was stripped from the [umbraco.cms.businesslogic.CMSNode] class
    /// It allows for getting Umbraco Node data with a non-static SqlHelper scope
    /// </summary>
    public class CmsNodeHelper
    {
        private readonly Guid _dataTypeObjectTypeId = Guid.Parse("30A2A501-1978-4DDB-A57B-F7EFED43BA3C");
        private readonly Guid _docTypeObjectTypeId = Guid.Parse("A2CB7800-F571-4787-9638-BC48539A0EFB");

        private const string m_SQLSingle =
            "SELECT id, createDate, trashed, parentId, nodeObjectType, nodeUser, level, path, sortOrder, uniqueID, text, nodeId as contentTypeId FROM umbracoNode  un left join cmsContentType ct on un.id = ct.nodeId WHERE id = @id";

        private const string m_SQMultiple =
            "SELECT id, createDate, trashed, parentId, nodeObjectType, nodeUser, level, path, sortOrder, uniqueID, text, nodeId as contentTypeId FROM umbracoNode  un left join cmsContentType ct on un.id = ct.nodeId WHERE id in ({0})";

        private const string m_SQLDescendants = @"
            SELECT id, createDate, trashed, parentId, nodeObjectType, nodeUser, level, path, sortOrder, uniqueID, text 
            FROM umbracoNode 
            WHERE path LIKE '%,{0},%'";

        private readonly string _connectionString;
        private ISqlHelper _sqlHelper;

        public ISqlHelper SqlHelper
        {
            get
            {
                if (_sqlHelper == null)
                {
                    try
                    {
                        _sqlHelper = DataLayerHelper.CreateSqlHelper(_connectionString);
                    }
                    catch
                    {
                    }
                }
                return _sqlHelper;
            }
        }

        public CmsNodeHelper(string connectionstring)
        {
            _connectionString = connectionstring;
        }

        /// <summary>
        /// Retrieves a list of all datatypedefinitions
        /// </summary>
        /// <returns>A list of all datatypedefinitions</returns>
        public IEnumerable<DataType> GetAllDataTypes()
        {
            var uniqueIds = GetAllUniquesFromObjectType(_dataTypeObjectTypeId);
            var result = GetNodesByIds<DataType>(uniqueIds);
            return result.OrderBy(x => x.Text);
        }

        public IEnumerable<DocType> GetAllDocTypes()
        {
            var uniqueIds = GetAllUniquesFromObjectType(_docTypeObjectTypeId);
            var result = GetNodesByIds<DocType>(uniqueIds);
            return result.OrderBy(x => x.Text);
        }

        public string[] GetAllTemplates(string connectionString)
        {
            const string queryString =
                @"select alias from cmsTemplate order by alias";

            var result = GetStringArrayFromQuery(connectionString, queryString);
            return result;
        }

        private string[] GetStringArrayFromQuery(string connectionString, string query)
        {
            var result = new List<string>();
            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(query, connection);
                try
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        result.Add(reader[0].ToString());
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return result.ToArray();
        }

        //public CmsNode GetNodeById(Guid uniqueId)
        //{
        //    CmsNode cmsNode;
        //    var id = SqlHelper.ExecuteScalar<int>("SELECT id FROM umbracoNode WHERE uniqueID = @uniqueId", SqlHelper.CreateParameter("@uniqueId", uniqueId));
        //    using (IRecordsReader dr = SqlHelper.ExecuteReader(m_SQLSingle,
        //            SqlHelper.CreateParameter("@id", id)))
        //    {
        //        if (dr.Read() || dr.IsNull("uniqueID"))
        //        {
        //            cmsNode = new CmsNode
        //                {
        //                    UniqueId = dr.GetGuid("uniqueID"),
        //                    NodeObjectType = dr.GetGuid("nodeObjectType"),
        //                    ContentTypeId = dr.GetInt("contentTypeId"),
        //                    Level = dr.GetShort("level"),
        //                    Path = dr.GetString("path"),
        //                    ParentId = dr.GetInt("parentId"),
        //                    Text = dr.GetString("text"),
        //                    SortOrder = dr.GetInt("sortOrder"),
        //                    UserId = dr.GetInt("nodeUser"),
        //                    CreateDate = dr.GetDateTime("createDate"),
        //                    IsTrashed = dr.GetBoolean("trashed")
        //                };
        //        }
        //        else
        //        {
        //            throw new ArgumentException(string.Format("No node exists with id '{0}' ({1})", id, uniqueId));
        //        }
        //    }

        //    return cmsNode;
        //}

        public IEnumerable<T> GetNodesByIds<T>(Guid[] uniqueIds) where T : CmsNode, new()
        {
            var cmsNodes = new List<T>();

            // TODO: parameterize this the "right" way to prevent future sql injection
            var strUIds = string.Format("'{0}'", string.Join("','", uniqueIds));

            var ids = new List<int>();
            using(IRecordsReader reader = SqlHelper.ExecuteReader(string.Format("SELECT id FROM umbracoNode WHERE uniqueID in ({0})", strUIds)))
            {
                while (reader.Read())
                {
                    ids.Add(reader.GetInt("id"));
                }
            }

            var strIds = string.Join(",", ids);

            using (IRecordsReader dr = SqlHelper.ExecuteReader(string.Format(m_SQMultiple,strIds)))
            {
                while(dr.Read())
                {
                    cmsNodes.Add(new T
                        {
                            UniqueId = dr.GetGuid("uniqueID"),
                            NodeObjectType = dr.GetGuid("nodeObjectType"),
                            ContentTypeId = dr.GetInt("contentTypeId"),
                            Level = dr.GetShort("level"),
                            Path = dr.GetString("path"),
                            ParentId = dr.GetInt("parentId"),
                            Text = dr.GetString("text"),
                            SortOrder = dr.GetInt("sortOrder"),
                            UserId = dr.GetInt("nodeUser"),
                            CreateDate = dr.GetDateTime("createDate"),
                            IsTrashed = dr.GetBoolean("trashed")
                        });
                }
            }

            if (typeof (T) == typeof (DocType))
            {
                // Fill out property data
                foreach (var node in cmsNodes)
                {
                    var docType = node as DocType;
                    if (docType != null) docType.Properties = GetPropertyTypes(node).ToList();
                }
            }

            return cmsNodes;
        }

        public IEnumerable<PropertyType> GetPropertyTypes(CmsNode node)
        {
            var result = new List<PropertyType>();
            using (var dr = SqlHelper.ExecuteReader(string.Format(
                @"select * from cmsPropertyType where contentTypeId = {0} order by sortOrder", node.ContentTypeId)))
            {
                while (dr.Read())
                    result.Add(new PropertyType
                        {
                            Alias = dr.GetString("Alias"),
                            Name = dr.GetString("Name"),
                            ContentTypeId = dr.GetInt("ContentTypeId"),
                            DataTypeId = dr.GetInt("dataTypeId"),
                            Id = dr.GetInt("id"),
                            SortOrder = dr.GetInt("sortOrder"),
                            Mandatory = dr.GetBoolean("mandatory"),
                            ValidationRegEx = dr.GetString("validationRegExp"),
                            Description = dr.GetString("Description")
                        });
            }

            return result;
        }
        
        /// <summary>
        /// Method for checking if a CMSNode exits with the given Guid
        /// </summary>
        /// <param name="uniqueID">Identifier</param>
        /// <returns>True if there is a CMSNode with the given Guid</returns>
        public bool IsNode(Guid uniqueID)
        {
            return
                (SqlHelper.ExecuteScalar<int>("select count(id) from umbracoNode where uniqueID = @uniqueID",
                                              SqlHelper.CreateParameter("@uniqueId", uniqueID)) > 0);
        }

        /// <summary>
        /// Method for checking if a CMSNode exits with the given id
        /// </summary>
        /// <param name="Id">Identifier</param>
        /// <returns>True if there is a CMSNode with the given id</returns>
        public bool IsNode(int Id)
        {
            return
                (SqlHelper.ExecuteScalar<int>("select count(id) from umbracoNode where id = @id",
                                              SqlHelper.CreateParameter("@id", Id)) > 0);
        }

        /// <summary>
        /// Retrieve a list of the unique id's of all CMSNodes given the objecttype
        /// </summary>
        /// <param name="objectType">The objecttype identifier</param>
        /// <returns>
        /// A list of all unique identifiers which each are associated to a CMSNode
        /// </returns>
        public Guid[] GetAllUniquesFromObjectType(Guid objectType)
        {
            IRecordsReader dr = SqlHelper.ExecuteReader("Select uniqueID from umbracoNode where nodeObjectType = @type",
                                                        SqlHelper.CreateParameter("@type", objectType));
            var tmp = new System.Collections.ArrayList();

            while (dr.Read()) tmp.Add(dr.GetGuid("uniqueID"));
            dr.Close();

            Guid[] retval = new Guid[tmp.Count];
            for (int i = 0; i < tmp.Count; i++) retval[i] = (Guid) tmp[i];
            return retval;
        }

        /// <summary>
        /// Retrieve a list of the node id's of all CMSNodes given the objecttype
        /// </summary>
        /// <param name="objectType">The objecttype identifier</param>
        /// <returns>
        /// A list of all node ids which each are associated to a CMSNode
        /// </returns>
        public int[] GetAllUniqueNodeIdsFromObjectType(Guid objectType)
        {
            IRecordsReader dr = SqlHelper.ExecuteReader("Select id from umbracoNode where nodeObjectType = @type",
                                                        SqlHelper.CreateParameter("@type", objectType));
            var tmp = new System.Collections.ArrayList();

            while (dr.Read()) tmp.Add(dr.GetInt("id"));
            dr.Close();

            return (int[]) tmp.ToArray(typeof (int));
        }


        /// <summary>
        /// Retrieves the top level nodes in the hierarchy
        /// </summary>
        /// <param name="ObjectType">The Guid identifier of the type of objects</param>
        /// <returns>
        /// A list of all top level nodes given the objecttype
        /// </returns>
        public Guid[] TopMostNodeIds(Guid ObjectType)
        {
            IRecordsReader dr =
                SqlHelper.ExecuteReader(
                    "Select uniqueID from umbracoNode where nodeObjectType = @type And parentId = -1 order by sortOrder",
                    SqlHelper.CreateParameter("@type", ObjectType));
            var tmp = new System.Collections.ArrayList();

            while (dr.Read()) tmp.Add(dr.GetGuid("uniqueID"));
            dr.Close();

            Guid[] retval = new Guid[tmp.Count];
            for (int i = 0; i < tmp.Count; i++) retval[i] = (Guid) tmp[i];
            return retval;
        }

        private int GetNewDocumentSortOrder(int parentId)
        {
            var sortOrder = 0;
            using (IRecordsReader dr = SqlHelper.ExecuteReader(
                "SELECT MAX(sortOrder) AS sortOrder FROM umbracoNode WHERE parentID = @parentID AND nodeObjectType = @GuidForNodesOfTypeDocument",
                SqlHelper.CreateParameter("@parentID", parentId),
                SqlHelper.CreateParameter("@GuidForNodesOfTypeDocument", Document._objectType)
                ))
            {
                while (dr.Read())
                    sortOrder = dr.GetInt("sortOrder") + 1;
            }

            return sortOrder;
        }

        /// <summary>
        /// Retrieve a list of the id's of all CMSNodes given the objecttype and the first letter of the name.
        /// </summary>
        /// <param name="objectType">The objecttype identifier</param>
        /// <param name="letter">Firstletter</param>
        /// <returns>
        /// A list of all CMSNodes which has the objecttype and a name that starts with the given letter
        /// </returns>
        protected int[] GetUniquesFromObjectTypeAndFirstLetter(Guid objectType, char letter)
        {
            using (
                IRecordsReader dr =
                    SqlHelper.ExecuteReader(
                        "Select id from umbracoNode where nodeObjectType = @objectType AND text like @letter",
                        SqlHelper.CreateParameter("@objectType", objectType),
                        SqlHelper.CreateParameter("@letter", letter.ToString() + "%")))
            {
                List<int> tmp = new List<int>();
                while (dr.Read()) tmp.Add(dr.GetInt("id"));
                return tmp.ToArray();
            }
        }
    }
}