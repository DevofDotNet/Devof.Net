using Blog.Domain.Interfaces;
using Blog.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Blog.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private IPostRepository? _posts;
    private ITagRepository? _tags;
    private ICommentRepository? _comments;
    private ILikeRepository? _likes;
    private IBookmarkRepository? _bookmarks;
    private IFollowRepository? _follows;
    private IReportRepository? _reports;
    private IUserRepository? _users;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IPostRepository Posts => _posts ??= new PostRepository(_context);
    public ITagRepository Tags => _tags ??= new TagRepository(_context);
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public ILikeRepository Likes => _likes ??= new LikeRepository(_context);
    public IBookmarkRepository Bookmarks => _bookmarks ??= new BookmarkRepository(_context);
    public IFollowRepository Follows => _follows ??= new FollowRepository(_context);
    public IFollowRepository Followers => Follows; // Alias for Follows
    public IReportRepository Reports => _reports ??= new ReportRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

