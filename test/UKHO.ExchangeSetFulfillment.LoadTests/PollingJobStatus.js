import { sleep } from 'k6';
import { getJobStatus, getJobBuild } from './Helper/ClientHelper.js';

const config = JSON.parse(open('../Config.json'));

/**
* Polls a job status until it's completed or times out
* @param {string} jobId - The ID of the job
* @returns {Object} - Object with job status details
*/
export function pollJobStatus(jobId) {
  let currentJobState = '';
  let currentBuildState = '';
  let elapsedSeconds = 0;
  const maxTimeToWait = config.MaxPollingTime; // 15 minutes in seconds
  const waitDuration = config.PollingInterval; // 5 seconds

  do {
    const jobStateResponse = getJobStatus(jobId);

    if (!jobStateResponse || !jobStateResponse.status) {
      console.error(`Failed to get status for job ${jobId}`);
      return { status: 'error', message: 'Failed to get job status' };
    }

    currentJobState = jobStateResponse.status;
    currentBuildState = jobStateResponse.buildStatus;

    console.log(`Job ${jobId} - State: ${currentJobState}, Build: ${currentBuildState}, Elapsed: ${elapsedSeconds}s`);

    if (currentJobState === 'completed') {
      break;
    }

    sleep(waitDuration);
    elapsedSeconds += waitDuration;
  } while (elapsedSeconds < maxTimeToWait);

  if (currentJobState !== 'completed') {
    console.warn(`Job ${jobId} did not complete within ${maxTimeToWait} seconds`);
    return { status: 'timeout', jobState: currentJobState, buildState: currentBuildState };
  }

  // Get build details
  const buildResponse = getJobBuild(jobId);

  if (!buildResponse || !buildResponse.builderExitCode) {
    console.error(`Failed to get build details for job ${jobId}`);
    return { status: 'error', message: 'Failed to get build details' };
  }

  return {
    status: 'completed',
    jobState: currentJobState,
    buildState: currentBuildState,
    builderExitCode: buildResponse.builderExitCode,
    builderSteps: buildResponse.builderSteps
  };
}