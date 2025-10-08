import { check } from 'k6';

import { 
  createJob,
  getJobStatus,
  getJobBuild
} from './JobOperations.js';

/**
* Creates a job
* @param {string} filter - The filter to use for job creation
* @param {string} jobSize - The size category of the job (Small, Medium)
* @returns {Object} - Object with job details
*/
export function create(filter, jobSize) {
  let jobResult = {};

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

  return Object.assign({
    jobId: jobResult.jobId,
    correlationId: jobResult.correlationId,
    filter: filter,
    size: jobSize
  });
}

/**
* Check the status of a job
* @returns {Object} - Response object with job status
*/
export function status(Id) {
  let jobResult = {};

  jobResult = getJobStatus(Id);

  //console.log("Job Status Response: " + JSON.stringify(jobResult));
  // Check job creation result
  if (!check(jobResult, {
    'Job status retrieved successfully': (r) => r.response.status === 200,
    'Response Body is having json value': (r) => r.response.json().jobState !== undefined
  }))
  {
    console.error(`Failed to get job status of job with jobId: ${Id}`);
    return {
      status: 'error',
      message: 'Failed to get job status'
    };
  }
  return Object.assign({
    status: jobResult.response.jobState,
    buildStatus: jobResult.response.buildState,
    response: jobResult.response
  });
}

/**
* Gets the build details of a job
* @returns {Object} - Response object with build details
*/
export function build(Id) {
  let jobResult = {};

  jobResult = getJobBuild(Id);

  // Check job build result
  if (!check(jobResult, {
    'Build details retrieved successfully': (r) => r.response.status === 200,
    'Response Body is having json value': (r) => r.response.json().builderExitCode !== undefined
  })) 
  {
    console.error(`Failed to get build details of job with ID: ${Id}`);
    return {
      status: 'error',
      message: 'Failed to get job build details'
    };
  }
  return Object.assign({
    builderExitCode: jobResult.response.builderExitCode,
    builderSteps: jobResult.response.builderSteps,
    response: jobResult.response
  });
}
