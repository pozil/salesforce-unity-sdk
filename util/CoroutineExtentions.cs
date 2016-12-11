using UnityEngine;
using System;
using System.Collections;

/**
 * These utility classes provides Coroutines with supports for return values and exceptions
 * Credit goes to @horsman from Twisted Oak Studios 
 */

/**
 * Extension of MonoBehavior that provides a StartCoroutine method which supports return values and exceptions
 */
public static class MonoBehaviorExt {
	public static Coroutine<T> StartCoroutine<T>(this MonoBehaviour obj, IEnumerator coroutine) {
		Coroutine<T> coroutineObject = new Coroutine<T>();
		coroutineObject.coroutine = obj.StartCoroutine(coroutineObject.internalRoutine(coroutine));
		return coroutineObject;
	}
}

/**
 * Generic Coroutine type that supports return values and exceptions
 */
public class Coroutine<T> {
	
	private T returnVal;
	private Exception e;

	public Coroutine coroutine;
	

    public T getValue() {
        if (e != null) {
            throw e;
        }
        return returnVal;
    }

	public IEnumerator internalRoutine(IEnumerator coroutine) {
		while (true) {
			try {
				if (!coroutine.MoveNext()) {
					yield break;
				}
			}
			catch (Exception e) {
				this.e = e;
				yield break;
			}
			object yielded = coroutine.Current;
			if (yielded != null && yielded.GetType() == typeof(T)) {
				returnVal = (T) yielded;
				yield break;
			}
			else {
				yield return coroutine.Current;
			}
		}
	}
}