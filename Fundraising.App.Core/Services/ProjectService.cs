﻿using AutoMapper;
using Fundraising.App.Core.Entities;
using Fundraising.App.Core.Interfaces;
using Fundraising.App.Core.Models;
using Fundraising.App.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fundraising.App.Core.Services
{
    public class ProjectService : IProjectService
    {

        private readonly IApplicationDbContext _dbContext;
        private readonly ILogger<ProjectService> _logger;
        private readonly IMapper _mapper;
        public ProjectService(IApplicationDbContext dbContext, ILogger<ProjectService> logger,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _logger = logger;
            _mapper = mapper;
        }

        // CREATE
        // --------------------------------------------------------
        public OptionsProject CreateProject(OptionsProject optionProject)
        {


            Project project = new()
            {
                Title = optionProject.Title,
                Description = optionProject.Description,
                CreatedDate = DateTime.Now,
                Category = optionProject.Category,
                TargetAmount = optionProject.TargetAmount,
                CreatorId = optionProject.CreatorId,
                ImagePath = optionProject.ImagePath
            };

            _dbContext.Projects.Add(project);
            _dbContext.SaveChanges();

            return new OptionsProject(project);
        }
        public async Task<Result<OptionsProject>> CreateProjectAsync(OptionsProject optionsProject)
        {
            if (optionsProject == null)
            {
                return new Result<OptionsProject>(ErrorCode.BadRequest, "Null options.");
            }
            if (string.IsNullOrWhiteSpace(optionsProject.Creator.ToString()) ||
               string.IsNullOrWhiteSpace(optionsProject.Description) ||
               string.IsNullOrWhiteSpace(optionsProject.AmountGathered.ToString()) ||
               string.IsNullOrWhiteSpace(optionsProject.TargetAmount.ToString()) ||
               string.IsNullOrWhiteSpace(optionsProject.Category.ToString()))
            {
                return new Result<OptionsProject>(ErrorCode.BadRequest, "Not all required project options provided.");
            }
            if (optionsProject.TargetAmount >= 0)
            {
                return new Result<OptionsProject>(ErrorCode.BadRequest, "Invalid Target Amount number.");
            }
            var newProject = new Project
            {
                Title = optionsProject.Title,
                Description = optionsProject.Description,
                CreatedDate = DateTime.Now,
                Category = optionsProject.Category,
                TargetAmount = optionsProject.TargetAmount,
                CreatorId = optionsProject.CreatorId,
                ImagePath = optionsProject.ImagePath
            };
            await _dbContext.Projects.AddAsync(newProject);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new Result<OptionsProject>(ErrorCode.InternalServerError, "Could not save project.");
            }
            return new Result<OptionsProject>
            {
                Data = new OptionsProject(newProject)
            };
        }

        // DELETE
        // --------------------------------------------------------
        public bool DeleteProject(int Id)
        {
            Project dbContextProject = _dbContext.Projects.Find(Id);
            if (dbContextProject == null) return false;
            _dbContext.Projects.Remove(dbContextProject);
            _dbContext.SaveChanges();
            return true;

        }

        public async Task<Result<int>> DeleteProjectAsync(int id)
        {
            var projectToDelete = await _dbContext.Projects.SingleOrDefaultAsync(project => project.Id == id);
            if (projectToDelete == null)
            {
                return new Result<int>(ErrorCode.NotFound, $"Project with id #{id} not found.");
            }
            _dbContext.Projects.Remove(projectToDelete);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new Result<int>(ErrorCode.InternalServerError, "Could not delete project.");
            }
            return new Result<int>
            {
                Data = id
            };
        }


        // READ / ALL
        // --------------------------------------------------------
        public List<OptionsProject> GetAllProjects()
        {
            List<Project> projects = _dbContext.Projects.ToList();
            List<OptionsProject> optionsProject = new();
            projects.ForEach(project => optionsProject.Add(new OptionsProject(project)));
            return optionsProject;
        }

        public async Task<Result<List<OptionsProject>>> GetAllProjectsAsync()
        {
            var projects = await _dbContext.Projects.ToListAsync();
            List<OptionsProject> optionsProjects = new();

            projects.ForEach(project =>
                optionsProjects.Add(new OptionsProject(project))
            );
            return new Result<List<OptionsProject>>
            {
                Data = optionsProjects.Count > 0 ? optionsProjects : new List<OptionsProject>()
            };
        }

        // READ / BY ID
        // --------------------------------------------------------
        public OptionsProject GetProjectById(int Id)
        {
            Project project = _dbContext.Projects.Find(Id);
            if (project == null)
            {
                return null;
            }
            return new OptionsProject(project);
        }
        public async Task<Result<OptionsProject>> GetProjectByIdAsync(int id)
        {
            if (id <0)
            {
                return new Result<OptionsProject>(ErrorCode.BadRequest, "CreatorId cannot be null.");
            }
            var project = await _dbContext
               .Projects
               .SingleOrDefaultAsync(pro => pro.Id == id);
            if (project == null)
            {
                return new Result<OptionsProject>(ErrorCode.NotFound, $"Product with CreatorId #{id} not found.");
            }
            return new Result<OptionsProject>
            {
                Data = new OptionsProject(project)
            };
        }

        // READ / BY CREATOR ID
        // --------------------------------------------------------
        public List<OptionsProject> GetProjectByCreatorId(string CreatorId)
        {
            List<OptionsProject> optionProjects = new();
            var projects = _dbContext.Projects.Where(project => project.CreatorId == CreatorId).ToList();
            projects.ForEach(project =>
                optionProjects.Add(new OptionsProject(project))
            );

            return optionProjects;

        }

        public async Task<Result<List<OptionsProject>>> GetProjectByCreatorIdAsync(string CreatorId)
        {
            if (CreatorId == null)
            {
                return new Result<List<OptionsProject>>(ErrorCode.BadRequest, "CreatorId cannot be null.");
            }
            var projects =  _dbContext
               .Projects
               .Where(pro => pro.CreatorId == CreatorId);
            if (projects == null)
            {
                return new Result<List<OptionsProject>>(ErrorCode.NotFound, $"Product with CreatorId #{CreatorId} not found.");
            }

            List<OptionsProject> optionsProjects = new();
            await projects.ForEachAsync(project=>
                optionsProjects.Add(new OptionsProject(project))
            );

            return new Result<List<OptionsProject>>
            {
                Data = optionsProjects
            };
        }

        // UPDATE
        // --------------------------------------------------------
        public OptionsProject UpdateProject(OptionsProject optionsProject, int Id)
        {
            Project dbContextProject = _dbContext.Projects.Find(Id);
            if (dbContextProject == null) return null;

            dbContextProject.Title = optionsProject.Title;
            dbContextProject.Description = optionsProject.Description;
            dbContextProject.Status = optionsProject.Status;
            dbContextProject.TargetAmount = optionsProject.TargetAmount;
            dbContextProject.Creator = optionsProject.Creator;


            _dbContext.SaveChanges();
            return new OptionsProject(dbContextProject);

        }
        public async Task<Result<OptionsProject>> UpdateProjectAsync(OptionsProject optionsProject , int id)
        {
            var projectToUpdate = await _dbContext.Projects.SingleOrDefaultAsync(proj => proj.Id == id);
            if ( projectToUpdate == null)
            {
                return new Result<OptionsProject>(ErrorCode.NotFound, $"Project with id #{id} not found.");
            }
            projectToUpdate.Title = optionsProject.Title;
            projectToUpdate.Creator = optionsProject.Creator;
            projectToUpdate.Description = optionsProject.Description;
            projectToUpdate.Status = optionsProject.Status;
            projectToUpdate.Category = optionsProject.Category;
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                return new Result<OptionsProject>(ErrorCode.InternalServerError, "Could not save project.");
            }
            return new Result<OptionsProject>
            {
                Data = new OptionsProject(projectToUpdate)
            };

        }

        public OptionsProject UpdateProjectAmount(OptionsProject optionsProject, int Id)
        {
            Project dbContextProject = _dbContext.Projects.Find(Id);
            if (dbContextProject == null) return null;

            dbContextProject.AmountGathered = optionsProject.AmountGathered;
           
            _dbContext.SaveChanges();
            return new OptionsProject(dbContextProject);

        }

        public OptionsProject UpdateProjectStatus(OptionsProject optionsProject, int Id)
        {
            Project dbContextProject = _dbContext.Projects.Find(Id);
            if (dbContextProject == null) return null;

            dbContextProject.Status = optionsProject.Status;

            _dbContext.SaveChanges();
            return new OptionsProject(dbContextProject);

        }


        // SEARCH BY TITLE
        // --------------------------------------------------------
        public async Task<Result<List<OptionsProject>>> GetProjectsSearchByTitleAsync(string title_search)
        {
            if(String.IsNullOrEmpty(title_search))
            {
                var AllProjects = await GetAllProjectsAsync();
                return new Result<List<OptionsProject>>
                {
                    Data = AllProjects.Data.Count > 0 ? AllProjects.Data : new List<OptionsProject>()
                };
            }
            var projects = await _dbContext.Projects.ToListAsync();
            var result_projects = projects.Where(x => x.Title.Contains(title_search)).ToList();
            List<OptionsProject> optionsProjects = new();

            result_projects.ForEach(project =>
                optionsProjects.Add(new OptionsProject(project))
            );
            return new Result<List<OptionsProject>>
            {
                Data = optionsProjects.Count > 0 ? optionsProjects : new List<OptionsProject>()
            };
        }

        public async Task<Result<List<OptionsProject>>> GetMyBackedProjectsAsync(string UserId)
        {

            if (UserId == null)
            {
                return new Result<List<OptionsProject>>(ErrorCode.BadRequest, "User cannot be null.");
            }

            // FIND ALL PAYMENTS USER MADE
            List<OptionPayment> mypayments = new();
            var payments = await _dbContext.Payments.ToListAsync();
            payments.ForEach(payment => {
                if (payment.MemberId == UserId)
                    mypayments.Add(new OptionPayment(payment));
            });

            // FIND THE RELEVANT REWARDS
            var rewards = await _dbContext.Rewards.ToListAsync();
            List<OptionReward> rewardsUserPayed = new();
            rewards.ForEach(reward => {
                mypayments.ForEach(payment => {
                    if (payment.RewardId == reward.Id)
                        rewardsUserPayed.Add(new OptionReward(reward));
                });
            });

            // FINALY FIND ALL THE PROJECTS THE USER BACKED 
            List<OptionsProject> projectsUserBacked = new();
            rewardsUserPayed.ForEach(reward =>  {
                var project = _dbContext.Projects.Where(pro => pro.Id == reward.ProjectId).ToList();
                projectsUserBacked.Add(new OptionsProject(project[0]));
            });

            // GET UNIQUE PROJECTS
            List<OptionsProject> returnDistinctProjects = new();
            projectsUserBacked.ForEach(project => {
                bool isUnique = true;
                returnDistinctProjects.ForEach(distinctProject => {
                    if (distinctProject.Id == project.Id)
                        isUnique = false;
                });
                if(isUnique)
                    returnDistinctProjects.Add(project);
            });

            return new Result<List<OptionsProject>>
            {
                Data =   returnDistinctProjects
            };

        }



        public Dictionary<OptionsProject, int> GetTrendingProjects(int top) 
        {
            var allProjects = _dbContext.Projects.ToList();
            var allRewards = _dbContext.Rewards.ToList();
            var allPayments = _dbContext.Payments.ToList();

            IDictionary<Project, int> projectPaymentsCounter = new Dictionary<Project, int>();

            // Initialize Dictionary (PROJECT_ID , PAYMENTS_COUNT)
            allProjects.ForEach(project => {
                projectPaymentsCounter.Add(project, 0);
            });


            // START COUNTING HOW MANY PAYMENTS EACH PROJECT HAS
            allProjects.ForEach(project => {
                // GET ALL REWARDS PER PROJECT
                var projectRewards = allRewards.Where(re => re.ProjectId == project.Id).ToList();
                projectRewards.ForEach(re => {
                    // GET ALL PAYMENTS PER REWARD
                    var rewardPaymets = allPayments.Where(pay => pay.RewardId == re.Id).ToList();
                    rewardPaymets.ForEach(pay => {
                        //OptionsProject optionsProject = new OptionsProject(project);
                        projectPaymentsCounter[project] = projectPaymentsCounter[project] + 1;
                    });
                });
            });

            var sortDict = from entry in projectPaymentsCounter orderby entry.Value descending select entry;
            var tempDict = sortDict.ToDictionary<KeyValuePair<Project, int>, Project, int>(pair => pair.Key, pair => pair.Value);
            
            // convert Projects to OptionProjects
            Dictionary<OptionsProject, int> ret_projectPaymentsCounter = new();
            for (int i = 0; i <tempDict.Count; i++) {
                if (i == top) break;
                ret_projectPaymentsCounter.Add(new OptionsProject(tempDict.ElementAt(i).Key), tempDict.ElementAt(i).Value);
                
            }

            return ret_projectPaymentsCounter;

        }

    }
}

