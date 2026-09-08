using System.Linq.Expressions;

namespace FileBrowser.Application.Common.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
}