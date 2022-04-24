#ifndef __MONO_MINI_INTERP_TIERING_H__
#define __MONO_MINI_INTERP_TIERING_H__

#include "interp-internals.h"

#define INTERP_TIER_ENTRY_LIMIT 1000

void
mono_interp_tiering_init (void);

InterpMethod*
mono_interp_tier_up_method (InterpMethod *imethod, ThreadContext *context);

void
mono_interp_register_imethod_data_items (gpointer *data_items, GSList *indexes);

void
mono_interp_register_imethod_patch_site (gpointer *imethod_ptr);

#endif /* __MONO_MINI_INTERP_TIERING_H__ */
