import { sleep } from 'k6';
import { 
  getJobStatus, 
  getJobBuild,
  calculateBuildTime,
  SmallJobTotalTime,
  MediumJobTotalTime,
  SmallJobBuildTime,
  MediumJobBuildTime
} from './ClientHelper.js';

const config = JSON.parse(open('../Config.json'));

/**
* Monitors a job until completion or timeout
* @param {string} jobId - The ID of the job
* @param {string} jobSize - The size category of the job (Small, Medium, Large)
* @param {number} startTime - The start time of the job in milliseconds
* @returns {Object} - Object with job details and status
*/
export function monitorJob(jobId, jobSize, startTime) {
  let jobState = '';
  let buildState = '';
  let builderExitCode = '';
  let builderSteps = [];
  let pollingTime = 0;

  // Keep polling until job is completed or timeout
  while (pollingTime < config.MaxPollingTime) {
    const statusResult = getJobStatus(jobId);
    jobState = statusResult.status;
    buildState = statusResult.buildStatus;

    // If job is completed, get build details
    if (jobState === 'completed') {
      const buildResult = getJobBuild(jobId);
      builderExitCode = buildResult.builderExitCode;
      builderSteps = buildResult.builderSteps;

      // If build is successful, record metrics and return
      if (builderExitCode === 'success') {
        const totalTime = (new Date().getTime() - startTime) / 1000; // in seconds
        const buildTime = calculateBuildTime(builderSteps);

        recordMetrics(jobSize, totalTime, buildTime);

        return {
          status: 'success',
          jobState: jobState,
          buildState: buildState,
          builderExitCode: builderExitCode,
          totalTime: totalTime,
          buildTime: buildTime
        };
      }

      // If build has failed, record and return
      if (builderExitCode === 'error') {
        return {
          status: 'error',
          jobState: jobState,
          buildState: buildState,
          builderExitCode: builderExitCode
        };
      }
    }

    // Sleep before next poll
    sleep(config.PollingInterval);
    pollingTime += config.PollingInterval;
  }

  // If we're still waiting after max polling time, extend if needed
  if (pollingTime >= config.MaxPollingTime && pollingTime < config.ExtendedPollingTime) {
    console.log(`Job ${jobId} is taking longer than expected. Extending polling time.`);

    while (pollingTime < config.ExtendedPollingTime) {
      const statusResult = getJobStatus(jobId);
      jobState = statusResult.status;
      buildState = statusResult.buildStatus;

      if (jobState === 'completed') {
        const buildResult = getJobBuild(jobId);
        builderExitCode = buildResult.builderExitCode;
        builderSteps = buildResult.builderSteps;

        if (builderExitCode === 'success' || builderExitCode === 'error') {
          const totalTime = (new Date().getTime() - startTime) / 1000;
          const buildTime = calculateBuildTime(builderSteps);

          recordMetrics(jobSize, totalTime, buildTime);

          return {
            status: builderExitCode === 'success' ? 'success' : 'error',
            jobState: jobState,
            buildState: buildState,
            builderExitCode: builderExitCode,
            totalTime: totalTime,
            buildTime: buildTime
          };
        }
      }

      sleep(config.PollingInterval);
      pollingTime += config.PollingInterval;
    }
  }

  // If we've reached here, job timed out
  return {
    status: 'timeout',
    jobState: jobState,
    buildState: buildState
  };
}

/**
* Records metrics for a job
* @param {string} jobSize - The size category of the job (Small, Medium, Large)
* @param {number} totalTime - The total time for the job in seconds
* @param {number} buildTime - The build time for the job in milliseconds
*/
function recordMetrics(jobSize, totalTime, buildTime) {
  switch(jobSize) {
    case 'Small':
      SmallJobTotalTime.add(totalTime);
      SmallJobBuildTime.add(buildTime);
      break;
    case 'Medium':
      MediumJobTotalTime.add(totalTime);
      MediumJobBuildTime.add(buildTime);
      break;
  }
}