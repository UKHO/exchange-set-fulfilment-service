import { sleep } from 'k6';
import { getJobStatus } from './Helper/ClientHelper.js';
const config = JSON.parse(open('../Config.json'));

export function pollJobStatus(jobId) {
  let currentJobState = '';
  let currentBuildState = '';
  let elapsedSeconds = 0;
  const maxTimeToWait = config.MaxPollingTime;
  const waitDuration = config.PollingInterval;

  do {
    const jobStateResponse = getJobStatus(jobId);
    if (!jobStateResponse || !jobStateResponse.status) {
      return { status: 'error', message: 'Failed to get job status' };
    }
    currentJobState = jobStateResponse.status;
    currentBuildState = jobStateResponse.buildStatus;
    if (currentJobState === 'completed') {
      break;
    }
    sleep(waitDuration);
    elapsedSeconds += waitDuration;
  } while (elapsedSeconds < maxTimeToWait);

  if (currentJobState !== 'completed') {
    return { status: 'timeout', jobState: currentJobState, buildState: currentBuildState };
  }
  return {
    status: 'completed',
    jobState: currentJobState,
    buildState: currentBuildState
  };
}