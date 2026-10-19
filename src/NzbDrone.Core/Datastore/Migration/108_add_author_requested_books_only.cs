using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(108)]
    public class add_author_requested_books_only : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            AddColumnIfMissing("Authors", "RequestedBooksOnly");
            AddColumnIfMissing("PendingAuthorImport", "RequestedBooksOnly");
        }

        private void AddColumnIfMissing(string tableName, string columnName)
        {
            if (Schema.Table(tableName).Exists() &&
                !Schema.Table(tableName).Column(columnName).Exists())
            {
                Alter.Table(tableName)
                    .AddColumn(columnName)
                    .AsBoolean()
                    .Nullable();
            }
        }
    }
}
