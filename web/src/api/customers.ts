import { apiClient } from './client';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '../types/customer';

export const customersApi = {
  list: (includeInactive = true) =>
    apiClient.get<Customer[]>('/customers', { params: { includeInactive } }).then(r => r.data),

  get: (id: string) =>
    apiClient.get<Customer>(`/customers/${id}`).then(r => r.data),

  create: (data: CreateCustomerRequest) =>
    apiClient.post<Customer>('/customers', data).then(r => r.data),

  update: (id: string, data: UpdateCustomerRequest) =>
    apiClient.put<Customer>(`/customers/${id}`, data).then(r => r.data),

  deactivate: (id: string) =>
    apiClient.delete(`/customers/${id}`),

  activate: (id: string) =>
    apiClient.post(`/customers/${id}/activate`),
};
