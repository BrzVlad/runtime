/**
 * \file
 * Threadpool for all concurrent GC work.
 *
 * Copyright (C) 2015 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "mono/sgen/sgen-gc.h"
#include "mono/sgen/sgen-thread-pool.h"
#include "mono/sgen/sgen-client.h"
#include "mono/utils/mono-os-mutex.h"

static mono_mutex_t lock;
static mono_cond_t work_cond;
static mono_cond_t done_cond;

static int threads_num;
static MonoNativeThreadId threads [SGEN_THREADPOOL_MAX_NUM_THREADS];
static int threads_context [SGEN_THREADPOOL_MAX_NUM_THREADS];

static volatile gboolean threadpool_shutdown;
static volatile int threads_finished;

static int contexts_num;
static SgenThreadPoolContext pool_contexts [SGEN_THREADPOOL_MAX_NUM_CONTEXTS];

enum {
	STATE_WAITING,
	STATE_IN_PROGRESS,
	STATE_DONE
};

static mono_native_thread_return_t
thread_func (void *data)
{
	return 0;
}

int
sgen_thread_pool_create_context (int num_threads, SgenThreadPoolThreadInitFunc init_func, SgenThreadPoolIdleJobFunc idle_func, SgenThreadPoolContinueIdleJobFunc continue_idle_func, SgenThreadPoolShouldWorkFunc should_work_func, void **thread_datas)
{
	return 0;
}

void
sgen_thread_pool_start (void)
{
}

void
sgen_thread_pool_shutdown (void)
{
}

SgenThreadPoolJob*
sgen_thread_pool_job_alloc (const char *name, SgenThreadPoolJobFunc func, size_t size)
{
	SgenThreadPoolJob *job = (SgenThreadPoolJob *)sgen_alloc_internal_dynamic (size, INTERNAL_MEM_THREAD_POOL_JOB, TRUE);
	job->name = name;
	job->size = size;
	job->state = STATE_WAITING;
	job->func = func;
	return job;
}

void
sgen_thread_pool_job_free (SgenThreadPoolJob *job)
{
	sgen_free_internal_dynamic (job, job->size, INTERNAL_MEM_THREAD_POOL_JOB);
}

void
sgen_thread_pool_job_enqueue (int context_id, SgenThreadPoolJob *job)
{
}

void
sgen_thread_pool_job_wait (int context_id, SgenThreadPoolJob *job)
{
}

void
sgen_thread_pool_idle_signal (int context_id)
{
}

void
sgen_thread_pool_idle_wait (int context_id, SgenThreadPoolContinueIdleWaitFunc continue_wait)
{
}

void
sgen_thread_pool_wait_for_all_jobs (int context_id)
{
}

/* Return 0 if is not a thread pool thread or the thread number otherwise */
int
sgen_thread_pool_is_thread_pool_thread (MonoNativeThreadId some_thread)
{
	return 0;
}

#endif
