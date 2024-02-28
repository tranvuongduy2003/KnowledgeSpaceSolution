using KnowledgeSpace.BackendServer.UnitTest.Helpers;

namespace KnowledgeSpace.BackendServer.UnitTest.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> input)
        {
            return new NotInDbSet<T>(input);
        }
    }
}
