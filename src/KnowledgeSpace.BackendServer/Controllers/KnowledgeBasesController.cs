using KnowledgeSpace.BackendServer.Authorization;
using KnowledgeSpace.BackendServer.Constants;
using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.BackendServer.Helpers;
using KnowledgeSpace.BackendServer.Services;
using KnowledgeSpace.ViewModels;
using KnowledgeSpace.ViewModels.Contents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace KnowledgeSpace.BackendServer.Controllers
{
    public class KnowledgeBasesController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ISequenceService _sequenceService;
        private readonly IStorageService _storageService;

        public KnowledgeBasesController(ApplicationDbContext context, ISequenceService sequenceService, IStorageService storageService)
        {
            _context = context;
            _sequenceService = sequenceService;
            _storageService = storageService;
        }

        #region KnowledgeBases
        [HttpPost]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.CREATE)]
        public async Task<IActionResult> PostKnowledgeBase([FromForm] KnowledgeBaseCreateRequest request)
        {
            var knowledgeBase = new KnowledgeBase()
            {
                CategoryId = request.CategoryId,
                Title = request.Title,
                SeoAlias = request.SeoAlias,
                Description = request.Description,
                Environment = request.Environment,
                Problem = request.Problem,
                StepToReproduce = request.StepToReproduce,
                ErrorMessage = request.ErrorMessage,
                Workaround = request.Workaround,
                Note = request.Note,
                Labels = request.Labels,
            };
            knowledgeBase.Id = await _sequenceService.GetKnowledgeBaseId();

            //Process attachment
            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var attachment in request.Attachments)
                {
                    var attachmentEntity = await SaveFile(knowledgeBase.Id, attachment);
                    _context.Attachments.Add(attachmentEntity);
                }
            }

            _context.KnowledgeBases.Add(knowledgeBase);

            //Process label
            if (!string.IsNullOrEmpty(request.Labels))
            {
                await ProcessLabel(request, knowledgeBase);
            }

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetById), new { id = knowledgeBase.Id }, request);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.VIEW)]
        public async Task<IActionResult> GetKnowledgeBases()
        {
            var knowledgeBases = _context.KnowledgeBases;

            var knowledgeBaseVms = await knowledgeBases.Select(kb => new KnowledgeBaseQuickVm()
            {
                Id = kb.Id,
                CategoryId = kb.CategoryId,
                Title = kb.Title,
                SeoAlias = kb.SeoAlias,
                Description = kb.Description,
            }).ToListAsync();

            return Ok(knowledgeBaseVms);
        }

        [HttpGet("filter")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.VIEW)]
        public async Task<IActionResult> GetKnowledgeBasesPaging([FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = _context.KnowledgeBases.AsQueryable();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.Title.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1 * pageSize))
                .Take(pageSize)
                .Select(kb => new KnowledgeBaseQuickVm()
                {
                    Id = kb.Id,
                    CategoryId = kb.CategoryId,
                    Title = kb.Title,
                    SeoAlias = kb.SeoAlias,
                    Description = kb.Description,
                })
                .ToListAsync();

            var pagination = new Pagination<KnowledgeBaseQuickVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.VIEW)]
        public async Task<IActionResult> GetById(int id)
        {
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
            if (knowledgeBase == null)
                return NotFound();

            var knowledgeBaseVm = new KnowledgeBaseVm()
            {
                Id = knowledgeBase.Id,
                CategoryId = knowledgeBase.CategoryId,
                Title = knowledgeBase.Title,
                SeoAlias = knowledgeBase.SeoAlias,
                Description = knowledgeBase.Description,
                Environment = knowledgeBase.Environment,
                Problem = knowledgeBase.Problem,
                StepToReproduce = knowledgeBase.StepToReproduce,
                ErrorMessage = knowledgeBase.ErrorMessage,
                Workaround = knowledgeBase.Workaround,
                Note = knowledgeBase.Note,
                OwnerUserId = knowledgeBase.OwnerUserId,
                Labels = knowledgeBase.Labels,
                CreateDate = knowledgeBase.CreateDate,
                LastModifiedDate = knowledgeBase.LastModifiedDate,
                NumberOfComments = knowledgeBase.NumberOfComments,
                NumberOfVotes = knowledgeBase.NumberOfVotes,
                NumberOfReports = knowledgeBase.NumberOfReports,
            };
            return Ok(knowledgeBaseVm);
        }

        [HttpPut("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.UPDATE)]
        public async Task<IActionResult> PutKnowledgeBase(int id, [FromBody] KnowledgeBaseCreateRequest request)
        {
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
            if (knowledgeBase == null)
                return NotFound();

            knowledgeBase.CategoryId = request.CategoryId;
            knowledgeBase.Title = request.Title;
            knowledgeBase.SeoAlias = request.SeoAlias;
            knowledgeBase.Description = request.Description;
            knowledgeBase.Environment = request.Environment;
            knowledgeBase.Problem = request.Problem;
            knowledgeBase.StepToReproduce = request.StepToReproduce;
            knowledgeBase.ErrorMessage = request.ErrorMessage;
            knowledgeBase.Workaround = request.Workaround;
            knowledgeBase.Note = request.Note;
            knowledgeBase.Labels = request.Labels;

            _context.KnowledgeBases.Update(knowledgeBase);

            //Process label
            if (!string.IsNullOrEmpty(request.Labels))
            {
                await ProcessLabel(request, knowledgeBase);
            }

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest();
        }

        [HttpDelete("{id}")]
        [ClaimRequirement(FunctionCode.CONTENT_KNOWLEDGEBASE, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteKnowledgeBase(int id)
        {
            var knowledgeBase = await _context.KnowledgeBases.FindAsync(id);
            if (knowledgeBase == null)
                return NotFound();

            _context.KnowledgeBases.Remove(knowledgeBase);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                var knowledgeBasevm = new KnowledgeBaseVm()
                {
                    Id = knowledgeBase.Id,
                    CategoryId = knowledgeBase.CategoryId,
                    Title = knowledgeBase.Title,
                    SeoAlias = knowledgeBase.SeoAlias,
                    Description = knowledgeBase.Description,
                    Environment = knowledgeBase.Environment,
                    Problem = knowledgeBase.Problem,
                    StepToReproduce = knowledgeBase.StepToReproduce,
                    ErrorMessage = knowledgeBase.ErrorMessage,
                    Workaround = knowledgeBase.Workaround,
                    Note = knowledgeBase.Note,
                    OwnerUserId = knowledgeBase.OwnerUserId,
                    Labels = knowledgeBase.Labels,
                    CreateDate = knowledgeBase.CreateDate,
                    LastModifiedDate = knowledgeBase.LastModifiedDate,
                    NumberOfComments = knowledgeBase.NumberOfComments,
                    NumberOfVotes = knowledgeBase.NumberOfVotes,
                    NumberOfReports = knowledgeBase.NumberOfReports,
                };
                return Ok(knowledgeBasevm);
            }
            return BadRequest();
        }

        private async Task ProcessLabel(KnowledgeBaseCreateRequest request, KnowledgeBase knowledgeBase)
        {

            string[] labels = request.Labels.Split(',');
            foreach (var labelText in labels)
            {
                var labelId = TextHelper.ToUnsignString(labelText);
                var existingLabel = await _context.Labels.FindAsync(labelId);
                if (existingLabel == null)
                {
                    var labelEntity = new Label()
                    {
                        Id = labelId,
                        Name = labelText,
                    };
                    _context.Labels.Add(labelEntity);
                }
                var labelInKnowledgeBase = new LabelInKnowledgeBase()
                {
                    KnowledgeBaseId = knowledgeBase.Id,
                    LabelId = labelId
                };
                _context.LabelInKnowledgeBases.Add(labelInKnowledgeBase);
            }
        }

        private async Task<Attachment> SaveFile(int knowledegeBaseId, IFormFile file)
        {
            var originalFileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            await _storageService.SaveFileAsync(file.OpenReadStream(), fileName);
            var attachmentEntity = new Attachment()
            {
                FileName = fileName,
                FilePath = _storageService.GetFileUrl(fileName),
                FileSize = file.Length,
                FileType = Path.GetExtension(fileName),
                KnowledgeBaseId = knowledegeBaseId,
            };
            return attachmentEntity;
        }
        #endregion

        #region Comments
        [HttpGet("{knowledgeBaseId}/comments")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.VIEW)]
        public async Task<IActionResult> GetComments(int knowledgeBaseId)
        {
            var comments = _context.Comments.Where(c => c.KnowledgeBaseId == knowledgeBaseId);

            var commentVms = await comments.Select(c => new CommentVm()
            {
                Id = c.Id,
                Content = c.Content,
                CreateDate = c.CreateDate,
                LastModifiedDate = c.LastModifiedDate,
                KnowledgeBaseId = c.KnowledgeBaseId,
                OwnwerUserId = c.OwnwerUserId
            }).ToListAsync();

            return Ok(commentVms);
        }

        [HttpGet("{knowledgeBaseId}/comments/filter")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.VIEW)]
        public async Task<IActionResult> GetCommentsPaging(int knowledgeBaseId, [FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = _context.Comments.Where(x => x.KnowledgeBaseId == knowledgeBaseId).AsQueryable();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.Content.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1 * pageSize))
                .Take(pageSize)
                .Select(c => new CommentVm()
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreateDate = c.CreateDate,
                    LastModifiedDate = c.LastModifiedDate,
                    KnowledgeBaseId = c.KnowledgeBaseId,
                    OwnwerUserId = c.OwnwerUserId,
                })
                .ToListAsync();

            var pagination = new Pagination<CommentVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{knowledgeBaseId}/comments/{commentId}")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.VIEW)]
        public async Task<IActionResult> GetCommentDetail(int knowledgeBaseId, int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound();

            var commentVm = new CommentVm()
            {
                Id = comment.Id,
                Content = comment.Content,
                CreateDate = comment.CreateDate,
                LastModifiedDate = comment.LastModifiedDate,
                KnowledgeBaseId = comment.KnowledgeBaseId,
                OwnwerUserId = comment.OwnwerUserId,
            };
            return Ok(commentVm);
        }

        [HttpPost("{knowledgeBaseId}/comments")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.CREATE)]
        public async Task<IActionResult> PostComment(int knowledgeBaseId, [FromBody] CommentCreateRequest request)
        {
            var comment = new Comment()
            {
                Content = request.Content,
                KnowledgeBaseId = request.KnowledgeBaseId,
                OwnwerUserId = string.Empty /*TODO: GET USER FROM CLAIM*/
            };
            _context.Comments.Add(comment);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest();
            }
            knowledgeBase.NumberOfComments = knowledgeBase.NumberOfComments.GetValueOrDefault(0) + 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetCommentDetail), new { id = knowledgeBaseId, commentId = comment.Id }, request);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPut("{knowledgeBaseId}/comments/{commentId}")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.UPDATE)]
        public async Task<IActionResult> PutComment(int knowledgeBaseId, int commentId, [FromBody] CommentCreateRequest request)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound();

            if (comment.OwnwerUserId != User.Identity.Name)
                return Forbid();

            comment.Content = request.Content;

            _context.Comments.Update(comment);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest();
        }

        [HttpDelete("{knowledgeBaseId}/comments/{commentId}")]
        [ClaimRequirement(FunctionCode.CONTENT_COMMENT, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteComment(int knowledgeBaseId, int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound();

            _context.Comments.Remove(comment);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest();
            }
            knowledgeBase.NumberOfComments = knowledgeBase.NumberOfComments.GetValueOrDefault(0) - 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                var commentVm = new CommentVm()
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    KnowledgeBaseId = comment.KnowledgeBaseId,
                    OwnwerUserId = comment.OwnwerUserId,
                    CreateDate = comment.CreateDate,
                    LastModifiedDate = comment.LastModifiedDate,
                };
                return Ok(commentVm);
            }
            return BadRequest();
        }
        #endregion

        #region Votes
        [HttpGet("{knowledgeBaseId}/votes")]
        public async Task<IActionResult> GetVotes(int knowledgeBaseId)
        {
            var votes = _context.Votes.Where(v => v.KnowledgeBaseId == knowledgeBaseId);

            var voteVms = await votes.Select(v => new VoteVm()
            {
                KnowledgeBaseId = v.KnowledgeBaseId,
                CreateDate = v.CreateDate,
                LastModifiedDate = v.LastModifiedDate,
                UserId = v.UserId,
            }).ToListAsync();

            return Ok(voteVms);
        }

        [HttpPost("{knowledgeBaseId}/votes")]
        public async Task<IActionResult> PostVote(int knowledgeBaseId, [FromBody] VoteCreateRequest request)
        {
            var vote = await _context.Votes.FindAsync(knowledgeBaseId, request.UserId);
            if (vote != null)
            {
                return BadRequest("This user has been voted for this KB");
            }

            vote = new Vote()
            {
                KnowledgeBaseId = request.KnowledgeBaseId,
                UserId = request.UserId,
            };
            _context.Votes.Add(vote);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest();
            }
            knowledgeBase.NumberOfVotes = knowledgeBase.NumberOfVotes.GetValueOrDefault(0) + 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpDelete("{knowledgeBaseId}/votes/{userId}")]
        public async Task<IActionResult> DeleteVote(int knowledgeBaseId, string userId)
        {
            var vote = await _context.Votes.FindAsync(knowledgeBaseId, userId);
            if (vote == null)
            {
                return NotFound("This user has not been voted for this KB");
            }

            _context.Votes.Remove(vote);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest();
            }
            knowledgeBase.NumberOfVotes = knowledgeBase.NumberOfVotes.GetValueOrDefault(0) - 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();
            if (result > 0)
            {
                return Ok();
            }
            return BadRequest();
        }
        #endregion

        #region Reports
        [HttpGet("{knowledgeBaseId}/reports")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.VIEW)]
        public async Task<IActionResult> GetReports(int knowledgeBaseId)
        {
            var reports = _context.Reports.Where(r => r.KnowledgeBaseId == knowledgeBaseId);

            var reportVms = await reports.Select(r => new ReportVm()
            {
                Id = r.Id,
                KnowledgeBaseId = r.KnowledgeBaseId,
                Content = r.Content,
                CreateDate = r.CreateDate,
                IsProcessed = r.IsProcessed,
                LastModifiedDate = r.LastModifiedDate,
                ReportUserId = r.ReportUserId,
            }).ToListAsync();

            return Ok(reportVms);
        }

        [HttpGet("{knowledgeBaseId}/reports/filter")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.VIEW)]
        public async Task<IActionResult> GetReportsPaging(int knowledgeBaseId, [FromQuery] string filter, [FromQuery] int pageIndex, [FromQuery] int pageSize)
        {
            var query = _context.Reports.Where(x => x.KnowledgeBaseId == knowledgeBaseId).AsQueryable();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(x => x.Content.Contains(filter));
            }
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1 * pageSize))
                .Take(pageSize)
                .Select(r => new ReportVm()
                {
                    Id = r.Id,
                    KnowledgeBaseId = r.KnowledgeBaseId,
                    Content = r.Content,
                    CreateDate = r.CreateDate,
                    IsProcessed = r.IsProcessed,
                    LastModifiedDate = r.LastModifiedDate,
                    ReportUserId = r.ReportUserId,
                })
                .ToListAsync();

            var pagination = new Pagination<ReportVm>
            {
                Items = items,
                TotalRecords = totalRecords,
            };
            return Ok(pagination);
        }

        [HttpGet("{knowledgeBaseId}/reports/{reportId}")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.VIEW)]
        public async Task<IActionResult> GetReportDetail(int knowledgeBaseId, int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound();

            var reportVm = new ReportVm()
            {
                Id = report.Id,
                KnowledgeBaseId = report.KnowledgeBaseId,
                Content = report.Content,
                CreateDate = report.CreateDate,
                IsProcessed = report.IsProcessed,
                LastModifiedDate = report.LastModifiedDate,
                ReportUserId = report.ReportUserId,
            };
            return Ok(reportVm);
        }

        [HttpPost("{knowledgeBaseId}/reports")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.CREATE)]
        public async Task<IActionResult> PostReport(int knowledgeBaseId, [FromBody] ReportCreateRequest request)
        {
            var report = new Report()
            {
                KnowledgeBaseId = request.KnowledgeBaseId,
                Content = request.Content,
                IsProcessed = false,
                ReportUserId = request.ReportUserId,
            };
            _context.Reports.Add(report);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest();
            }
            knowledgeBase.NumberOfReports = knowledgeBase.NumberOfReports.GetValueOrDefault(0) + 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return CreatedAtAction(nameof(GetReportDetail), new { id = knowledgeBaseId, reportId = report.Id }, request);
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpPut("{knowledgeBaseId}/reports/{reportId}")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.UPDATE)]
        public async Task<IActionResult> PutReport(int knowledgeBaseId, int reportId, [FromBody] ReportCreateRequest request)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound();

            if (report.ReportUserId != User.Identity.Name)
                return Forbid();

            report.Content = request.Content;

            _context.Reports.Update(report);
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest();
        }

        [HttpDelete("{knowledgeBaseId}/reports/{reportId}")]
        [ClaimRequirement(FunctionCode.CONTENT_REPORT, CommandCode.DELETE)]
        public async Task<IActionResult> DeleteReport(int knowledgeBaseId, int reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null)
                return NotFound();

            _context.Reports.Remove(report);

            var knowledgeBase = await _context.KnowledgeBases.FindAsync(knowledgeBaseId);
            if (knowledgeBase == null)
            {
                return BadRequest();
            }
            knowledgeBase.NumberOfReports = knowledgeBase.NumberOfReports.GetValueOrDefault(0) - 1;
            _context.KnowledgeBases.Update(knowledgeBase);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return Ok();
            }
            return BadRequest();
        }
        #endregion

        #region Attachments
        [HttpGet("{knowledgeBaseId}/attachments")]
        public async Task<IActionResult> GetAttachments(int knowledgeBaseId)
        {
            var attachments = _context.Attachments.Where(a => a.KnowledgeBaseId == knowledgeBaseId);

            var attachmentVms = await attachments.Select(a => new AttachmentVm()
            {
                Id = a.Id,
                KnowledgeBaseId = a.KnowledgeBaseId,
                CreateDate = a.CreateDate,
                FileName = a.FileName,
                FilePath = a.FilePath,
                FileSize = a.FileSize,
                FileType = a.FileType,
                LastModifiedDate = a.LastModifiedDate
            }).ToListAsync();

            return Ok(attachmentVms);
        }

        [HttpDelete("{knowledgeBaseId}/attachments/{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(int knowledgeBaseId, int attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
                return NotFound();

            _context.Attachments.Remove(attachment);

            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return Ok();
            }
            return BadRequest();
        }
        #endregion
    }
}
