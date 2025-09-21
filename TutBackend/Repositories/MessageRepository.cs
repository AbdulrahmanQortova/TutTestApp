using TutBackend.Data;
using Tut.Common.Models;

namespace TutBackend.Repositories;

public class MessageRepository(TutDbContext context) : Repository<GMessage>(context), IMessageRepository;

