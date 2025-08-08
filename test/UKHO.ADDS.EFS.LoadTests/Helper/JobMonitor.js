import { sleep } from 'k6';
import { 
  getJobStatus
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
  let pollingTime = 0;

  // Keep polling until job is completed or timeout
  while (pollingTime < config.MaxPollingTime) {
    const statusResult = getJobStatus(jobId, jobSize);
    jobState = statusResult.status;
    buildState = statusResult.buildStatus;

    // If job is completed, get build details
    if (jobState === 'completed') {
      const totalTime = (new Date().getTime() - startTime) / 1000; // in seconds
      return {
        status: 'success',
        jobState: jobState,
        buildState: buildState,
        totalTime: totalTime,
        buildTime: null // Always include buildTime for downstream code
      };
    }
    // Sleep before next poll
    sleep(config.PollingInterval);
    pollingTime += config.PollingInterval;
  }

  // If we're still waiting after max polling time, extend if needed
  if (pollingTime >= config.MaxPollingTime && pollingTime < config.ExtendedPollingTime) {
    console.log(`Job ${jobId} is taking longer than expected. Extending polling time.`);
    while (pollingTime < config.ExtendedPollingTime) {
      const statusResult = getJobStatus(jobId, jobSize);
      jobState = statusResult.status;
      buildState = statusResult.buildStatus;
      if (jobState === 'completed') {
        const totalTime = (new Date().getTime() - startTime) / 1000;
        return {
          status: 'success',
          jobState: jobState,
          buildState: buildState,
          totalTime: totalTime,
          buildTime: null
        };
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
