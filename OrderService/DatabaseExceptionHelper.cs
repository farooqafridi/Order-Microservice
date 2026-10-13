using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace OrderService
{
    public static class DatabaseExceptionHelper
    {
        public static bool IsIdempotencyUniqueConstraintViolation(
            DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601
                    || sqlException.Number == 2627);
        }
    }
}
