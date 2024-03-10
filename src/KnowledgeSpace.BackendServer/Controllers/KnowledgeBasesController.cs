﻿using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.BackendServer.Data.Entities;
using KnowledgeSpace.BackendServer.Services;
using KnowledgeSpace.ViewModels;
using KnowledgeSpace.ViewModels.Contents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeSpace.BackendServer.Controllers
{
    public class KnowledgeBasesController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ISequenceService _sequenceService;

        public KnowledgeBasesController(ApplicationDbContext context, ISequenceService sequenceService)
        {
            _context = context;
            _sequenceService = sequenceService;
        }

        [HttpPost]
        public async Task<IActionResult> PostKnowledgeBase([FromBody] KnowledgeBaseCreateRequest request)
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
            _context.KnowledgeBases.Add(knowledgeBase);
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
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                return NoContent();
            }
            return BadRequest();
        }

        //URL: DELETE: http://localhost:5001/api/knowledgeBases/{id}
        [HttpDelete("{id}")]
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

        [HttpGet("{knowledgeBaseId}/comments/filter")]
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
        public async Task<IActionResult> GetCommentDetail(int commentId)
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
        public async Task<IActionResult> PostComment(int knowledgeBaseId, [FromBody] CommentCreateRequest request)
        {
            var comment = new Comment()
            {
                Content = request.Content,
                KnowledgeBaseId = request.KnowledgeBaseId,
                OwnwerUserId = string.Empty /*TODO: GET USER FROM CLAIM*/
            };
            _context.Comments.Add(comment);
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
        public async Task<IActionResult> PutComment(int commentId, [FromBody] CommentCreateRequest request)
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
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound();

            _context.Comments.Remove(comment);
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
    }
}
