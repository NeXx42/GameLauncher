using System.Text;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Objects.Tags;

namespace GameLibrary.Logic.Objects;

public struct GameFilterRequest
{
    public enum OrderType
    {
        Id,
        Name,
        LastPlayed,
    }

    public string? nameFilter;
    public HashSet<TagDto>? tagList;

    public OrderType orderType;
    public bool orderDirection;

    public int page;
    public int contentPerPage;

    public string ConstructSQL()
    {
        StringBuilder sql = new StringBuilder($"SELECT g.*, count(*) OVER() as total_count FROM {dbo_Game.tableName} g ");

        List<string> joinClause = new List<string>();
        List<string> whereClause = new List<string>();
        List<string> groupClause = new List<string>();
        List<string> havingClause = new List<string>();

        // active filter, may allow you to filter uninstalled steam games?
        whereClause.Add($"g.{nameof(dbo_Game.status)} = {(int)GameDto.Status.Active}");

        if (!string.IsNullOrEmpty(nameFilter))
        {
            whereClause.Add($"{nameof(dbo_Game.gameName)} like '{nameFilter}%'");
        }

        if (tagList?.Count > 0)
        {
            List<TagDto_Managed> managedTags = new List<TagDto_Managed>(tagList.Count);
            List<int> unmanagedTags = new List<int>(tagList.Count);

            foreach (TagDto tag in tagList)
            {
                if (tag is TagDto_Managed managed)
                {
                    managedTags.Add(managed);
                }
                else
                {
                    unmanagedTags.Add(tag.id);
                }
            }

            if (unmanagedTags.Count > 0)
            {
                joinClause.Add($"JOIN {dbo_GameTag.tableName} gt ON gt.{nameof(dbo_GameTag.GameId)} = {nameof(dbo_Game.id)}");
                whereClause.Add($"gt.{nameof(dbo_GameTag.TagId)} in ({string.Join(",", unmanagedTags)})");

                groupClause.Add($"g.{nameof(dbo_Game.id)}");
                havingClause.Add($"COUNT(DISTINCT gt.{nameof(dbo_GameTag.TagId)}) = {unmanagedTags.Count}");
            }

            foreach (TagDto_Managed managedTag in managedTags)
            {
                managedTag.HandleFilterSQL(ref joinClause, ref whereClause, ref groupClause, ref havingClause);
            }
        }

        if (joinClause.Count > 0)
        {
            sql.Append(string.Join(" ", joinClause));
        }

        if (whereClause.Count > 0)
        {
            sql.Append(" WHERE ");
            sql.Append(string.Join(" AND ", whereClause));
        }

        if (groupClause.Count > 0)
        {
            sql.Append(" GROUP BY ");
            sql.Append(string.Join(" AND ", groupClause));
        }

        if (havingClause.Count > 0)
        {
            sql.Append(" HAVING ");
            sql.Append(string.Join(" AND ", havingClause));
        }

        sql.Append(CreateOrderBy());
        sql.Append(CreateLimit());

        string rawSql = sql.ToString();
        return rawSql;
    }

    private StringBuilder CreateOrderBy()
    {
        StringBuilder sql = new StringBuilder(" ORDER BY ");
        switch (orderType)
        {
            case OrderType.Id: sql.Append($"g.{nameof(dbo_Game.id)}"); break;
            case OrderType.Name: sql.Append($"g.{nameof(dbo_Game.gameName)}"); break;
            case OrderType.LastPlayed: sql.Append($"g.{nameof(dbo_Game.lastPlayed)}"); break;
        }

        sql.Append(orderDirection ? " ASC" : " DESC");
        return sql;
    }

    private string CreateLimit()
    {
        int skip = contentPerPage * page;
        return $" LIMIT {contentPerPage} OFFSET {skip};";
    }
}