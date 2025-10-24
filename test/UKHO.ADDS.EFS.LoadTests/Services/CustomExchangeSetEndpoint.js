import http from 'k6/http';
import { check } from 'k6';
import { Trend } from 'k6/metrics';
import { generateCorrelationId } from './JobOperations.js';

const config = JSON.parse(open('../Config.json'));

// Trends for tracking Custom Exchange Set  metrics
export const SmallProductNamesResponseTime = new Trend('SmallProductNamesResponseTime');
export const MediumProductNamesResponseTime = new Trend('MediumProductNamesResponseTime');
export const SmallProductVersionsResponseTime = new Trend('SmallProductVersionsResponseTime');
export const MediumProductVersionsResponseTime = new Trend('MediumProductVersionsResponseTime');
export const SmallUpdateSinceResponseTime = new Trend('SmallUpdateSinceResponseTime');
export const MediumUpdateSinceResponseTime = new Trend('MediumUpdateSinceResponseTime');

// Trends for tracking API response times
export const ProductNamesResponseTime = new Trend('ProductNamesResponseTime');
export const ProductVersionsResponseTime = new Trend('ProductVersionsResponseTime');
export const UpdateSinceResponseTime = new Trend('UpdateSinceResponseTime');

export function getProductNames(customData, customExchangeSetSize) {

    const correlationId = generateCorrelationId();
    const payload = JSON.stringify(customData);

    const url = `${config.Base_URL}${config.ProductNamesEndpoint}`;

    const params = {
        headers: {
            'X-Correlation-Id': correlationId,
            'Authorization': `Bearer ${config.EFSToken}`,
            'Content-Type': 'application/json'
        }
    };

    const response = http.post(url, payload, params);
    
    check(response, {
        'Product Names created successfully': (r) => r.status === 202,
        'Product Names ID is present': (r) => r.json().fssBatchId !== undefined
    });

    ProductNamesResponseTime.add(response.timings.duration);

    // Record ProductNames creation time
    switch (customExchangeSetSize) {
        case 'Small':
            SmallProductNamesResponseTime.add(response.timings.duration);
            break;
        case 'Medium':
            MediumProductNamesResponseTime.add(response.timings.duration);
            break;
    }

    return {
        fssBatchId: response.json().fssBatchId,
        correlationId: correlationId,
        response: response
    };

}

export function getProductVersions(customData, customExchangeSetSize) {

    const correlationId = generateCorrelationId();
    const payload = JSON.stringify(customData);

    const url = `${config.Base_URL}${config.ProductVersionsEndpoint}`;

    const params = {
        headers: {
            'X-Correlation-Id': correlationId,
            'Authorization': `Bearer ${config.EFSToken}`,
            'Content-Type': 'application/json'
        }
    };

    const response = http.post(url, payload, params);
   
    check(response, {
        'Product Versions created successfully': (r) => r.status === 202,
        'Product Versions ID is present': (r) => r.json().fssBatchId !== undefined
    });

    ProductVersionsResponseTime.add(response.timings.duration);

    // Record ProductVersions creation time
    switch (customExchangeSetSize) {
        case 'Small':
            SmallProductVersionsResponseTime.add(response.timings.duration);
            break;
        case 'Medium':
            MediumProductVersionsResponseTime.add(response.timings.duration);
            break;
    }

    return {
        fssBatchId: response.json().fssBatchId,
        correlationId: correlationId,
        response: response
    };

}

export function updateSince(customData, customExchangeSetSize) {

    const correlationId = generateCorrelationId();
    const payload = JSON.stringify(customData);

    const url = `${config.Base_URL}${config.UpdatesSinceEndpoint}`;

    const params = {
        headers: {
            'X-Correlation-Id': correlationId,
            'Authorization': `Bearer ${config.EFSToken}`,
            'Content-Type': 'application/json'
        }
    };

    const response = http.post(url, payload, params);
    
    check(response, {
        'Update Since created successfully': (r) => r.status === 202,
        'Update Since ID is present': (r) => r.json().fssBatchId !== undefined
    });

    UpdateSinceResponseTime.add(response.timings.duration);

    // Record updatesSince creation time
    switch (customExchangeSetSize) {
        case 'Small':
            SmallUpdateSinceResponseTime.add(response.timings.duration);
            break;
        case 'Medium':
            MediumUpdateSinceResponseTime.add(response.timings.duration);
            break;
    }

    return {
        fssBatchId: response.json().fssBatchId,
        correlationId: correlationId,
        response: response
    };

}
