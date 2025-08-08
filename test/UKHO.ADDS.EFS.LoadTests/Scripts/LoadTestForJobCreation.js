import { check } from 'k6';

import { 
  createJob,
  SmallJobDurationTime,
  MediumJobDurationTime,
  JobDurationTime
} from '../Helper/ClientHelper.js';
import { monitorJob } from '../Helper/JobMonitor.js';

/**
* Creates a job and monitors it until completion
* @param {string} filter - The filter to use for job creation
* @param {string} jobSize - The size category of the job (Small, Medium)
* @returns {Object} - Object with job details and monitoring results
*/
export function createAndMonitorJob(filter, jobSize) {
  let jobResult = {};
  let startTime = new Date().getTime();

  jobResult = createJob(filter, jobSize);

  // Check job creation result
  if (!check(jobResult, {
    'Job created successfully': (r) => r.response && r.response.status === 200,
    'Job ID is present': (r) => r.jobId !== undefined
  })) {
    console.error(`Failed to create ${jobSize} job with filter: ${filter}`);
    return {
      status: 'error',
      message: 'Failed to create job'
    };
  }

  console.log(`Created ${jobSize} job with ID: ${jobResult.jobId}`);

  // Monitor job until completion
  const monitorResult = monitorJob(jobResult.jobId, jobSize, startTime);
  const jobDuration = (new Date().getTime() - startTime); // in seconds

  JobDurationTime.add(jobDuration);

  // Record job creation time
  switch(jobSize) {
    case 'Small':
      SmallJobDurationTime.add(jobDuration);
      break;
    case 'Medium':
      MediumJobDurationTime.add(jobDuration);
      break;
  }
  return Object.assign({
    jobId: jobResult.jobId,
    correlationId: jobResult.correlationId,
    filter: filter,
    size: jobSize
  }, monitorResult);
}
