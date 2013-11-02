//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace DNA.Data.Documents
{
    /// <summary>
    /// Reprsent an entity serializer for bson data format.
    /// </summary>
    public class BSonSerializer : IEntitySerializer
    {
        /// <summary>
        /// Convert the entity (document) to byte array.
        /// </summary>
        /// <param name="entity">Entity.</param>
        public byte[] Serialize<T>(T entity)
        {
            var serializer = new JsonSerializer();
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.All;
            
            using (var memoryStream = new MemoryStream())
            using (var writer = new BsonWriter(memoryStream))
            {
                serializer.Serialize(writer, entity, typeof(T));
                return memoryStream.ToArray();
            }
            //var jsonString = JsonConvert.SerializeObject(entity);
            //return Encoding.UTF8.GetBytes(jsonString);
        }
        /// <summary>
        /// Converts the byte array into an entity (document).
        /// </summary>
        /// <param name="bytes">BSON Bytes.</param>
        public T Deserialize<T>(byte[] bytes)
        {
            //var jsonString = Encoding.UTF8.GetString(bytes);
            //return JsonConvert.DeserializeObject<T>(jsonString);
            var serializer = new JsonSerializer();
            serializer.PreserveReferencesHandling = PreserveReferencesHandling.All;
            using (var memoryStream = new MemoryStream(bytes))
            {
                var reader = new BsonReader(memoryStream);
                var entity = serializer.Deserialize<T>(reader);
                return entity;
            }
        }
    }

}
