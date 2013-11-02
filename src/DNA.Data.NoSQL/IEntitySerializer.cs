//  Copyright (c) 2012 Ray Liang (http://www.dotnetage.com)
//  Licensed under the GPLv2: https://dotnetage.codeplex.com/license


namespace DNA.Data.Documents
{
    public interface IEntitySerializer
    {
        byte[] Serialize<T>(T entity);

        T Deserialize<T>(byte[] bytes);
    }
}
