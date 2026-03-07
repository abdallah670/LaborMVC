
namespace LaborDAL.Repo.Abstract
{
    public interface IRatingRepo: IRepository<Rating>
    {
        Task<List<Rating>> GetAllRatingByUserId(string userid);
    }
}
