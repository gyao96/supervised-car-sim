using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
struct Key
{
    Vector2 goal_spec;
    float beta;
    bool goal_stuck;
}

enum Action { LEFT, RIGHT, DOWN, UP, UP_LEFT, UP_RIGHT, DOWN_RIGHT, DOWN_LEFT, ABSORB };

public class MDP
{
    private float gridWidth;
    private int gridDimension;
    private int S = 3; // The number of states
    private int A = 8; // The number of actions
    private int goal_state;
    private int stepTime;
    private float beta = 200.0f;
    private float BETA_UPDATE_STEP = 0.1f;


    /* The default reward of any state-action pair
                (s, a). This is reward yielded by any legal state-action pair
                that is unaffected by reward_dict. */
    private float default_reward = -1.0f;
    private float[,] reward;

    private float[,] action_probs;
    private float[,] transition_probs;

    private int[,] transition_cached;
    private float[,] Q;
    private float[] thisState;


    public MDP(float aGridWidth, int aGridDimension, int aStepTime)
    {
        gridWidth = aGridWidth;
        gridDimension = aGridDimension;
        stepTime = aStepTime;
        S = gridDimension * gridDimension;
        A = 9;
        reward = new float[S, A];
        transition_cached = new int[S, A];
        Q = new float[S, A];
        thisState = new float[S];
        action_probs = new float[S, A];
        transition_probs = new float[S, S];
    }

    public void init(Vector3 goal, float aBeta, float aBETA_UPDATE_STEP)
    {
        beta = aBeta;
        BETA_UPDATE_STEP = aBETA_UPDATE_STEP;
        /* Assign goal state */
        Vector2Int goal_grid_cord = position_to_cord(goal);
        if (!validCord(goal_grid_cord.x, goal_grid_cord.y))
        {
            Debug.LogError("Goal Positon out of bound!\n");
        }
        goal_state = cord_to_state(goal_grid_cord.x, goal_grid_cord.y);

        /* Cache Transition & Assign Reward Dict */
        for (int s = 0; s < S; s++)
        {
            for (int a = 0; a < A; a++)
            {
                int s_prime = transition(s, (Action)a);
                transition_cached[s, a] = s_prime;
                if (s_prime == s)
                {
                    reward[s, a] = float.MinValue;
                }
                else
                {
                    reward[s, a] = default_reward;
                    // cache the neighbors
                }
            }
        }

        /* Calculate Q-values */
        qValues(false);

        /* Calculate Action Probabilities */
        //actionProbabilities();
        transitionProbabilities();

        return;
    }

    public float[] predictThisState(Vector3 hostPosition)
    {
        Vector2Int hostCord = position_to_cord(hostPosition);
        int hostState = cord_to_state(hostCord.x, hostCord.y);
        for (int s = 0; s < S; s++)
            thisState[s] = 0.0f;
        thisState[hostState] = 1.0f;
        List<int> props = new List<int> { hostState };
        bool[] travesed = new bool[S];
        travesed[hostState] = true;
        int i = 0;
        while (props.Count > 0 && i < stepTime)
        {
            List<int> next_props = new List<int> { };
            foreach (int s in props)
            {
                for (int a = 0; a < A; a++)
                {
                    int s_prime = transition_cached[s, a];
                    thisState[s_prime] += thisState[s] * transition_probs[s_prime, s];
                    if (travesed[s_prime] == false)
                    {
                        next_props.Add(s_prime);
                        travesed[s_prime] = true;
                    }
                }
            }
            props = next_props;
            i++;
        }
        thisState[hostState] -= 1.0f;
        return thisState;
    }

    public void updateBeta(float aBeta, int aStepTIme)
    {
        if (Mathf.Abs(aBeta - beta) > BETA_UPDATE_STEP || stepTime != aStepTIme)
        {
            beta = aBeta;
            stepTime = aStepTIme;
            transitionProbabilities();
        }
    }

    private void transitionProbabilities()
    {
        actionProbabilities();
        for (int s = 0; s < S; s++)
        {
            for (int ss = 0; ss < S; ss++)
            {
                transition_probs[s, ss] = 0.0f;
            }
        }
        for (int s = 0; s < S; s++)
        {
            for (int a = 0; a < A; a++)
            {
                int s_prime = transition_cached[s, a];
                transition_probs[s_prime, s] += action_probs[s, a];
            }
        }
    }

    private void actionProbabilities()
    {
        float[,] Q_copy = new float[S, A];
        float[] amax = new float[S];
        for (int s = 0; s < S; s++)
        {
            amax[s] = float.MinValue;
            for (int a = 0; a < A; a++)
            {
                Q_copy[s, a] = Q[s, a] / beta;
                amax[s] = Mathf.Max(amax[s], Q_copy[s, a]);
            }
            if(amax[s] <= float.MinValue + float.Epsilon)
            {
                Debug.LogWarning("Warning: There are states without any legal actions");
                amax[s] = 0.0f;
            }
        }
        for (int s = 0; s < S; s++)
        {
            for (int a = 0; a < A; a++)
            {
                Q_copy[s, a] -= amax[s];
                Q_copy[s, a] = Mathf.Exp(Q_copy[s, a]);
            }
        }

        /* Normalize */
        for (int s = 0; s < S; s++)
        {
            float denom = 0.0f;
            for (int a = 0; a < A; a++)
            {
                denom += Q_copy[s, a];
            }
            for (int a = 0; a < A; a++)
            {
                action_probs[s, a] = Q_copy[s, a] / denom;
            }
        }
    }
    private void qValues(bool goal_stuck)
    {
        for (int s = 0; s < S; s++)
        {
            for (int a = 0; a < A; a++)
            {
                Q[s, a] = float.MinValue;
            }
        }
        for (int s = 0; s < S; s++)
        {
            if (s == goal_state && goal_stuck)
            {
                Q[s, (int)Action.ABSORB] = 0;
                continue;
            }
            for (int a = 0; a < A; a++)
            {
                if (s == goal_state && a == (int)Action.ABSORB)
                    Q[s, a] = 0;
                else
                    Q[s, a] = reward[s, a] + goalDistance(transition_cached[s, a]);
            }
        }
        return;
    }

    private float goalDistance(int s)
    {
        Vector2 sV = state_to_cord(s);
        Vector2 sG = state_to_cord(goal_state);
        float distance = gridWidth * (sV - sG).sqrMagnitude;
        return -distance;
    }

    private Vector2Int position_to_cord(Vector3 pos)
    {
        int offset = gridDimension / 2;
        return new Vector2Int(Mathf.FloorToInt(pos.x / gridWidth) + offset, Mathf.FloorToInt(pos.z / gridWidth) + offset);
    }

    private bool validCord(int g_x, int g_y)
    {
        return g_x >= 0 && g_x < gridDimension && g_y >= 0 && g_y < gridDimension;
    }

    private int cord_to_state(int g_x, int g_y)
    {
        Assert.IsTrue(g_y * gridDimension + g_x < S);
        return g_y * gridDimension + g_x;
    }

    private Vector2Int state_to_cord(int s)
    {
        Assert.IsTrue(s >= 0 && s < gridDimension * gridDimension);
        return new Vector2Int(s % gridDimension, s / gridDimension);
    }

    private Action invertAction(Action a)
    {
        switch (a)
        {
            case Action.ABSORB: return Action.ABSORB;
            case Action.LEFT: return Action.RIGHT;
            case Action.RIGHT: return Action.LEFT;
            case Action.DOWN: return Action.UP;
            case Action.UP: return Action.DOWN;
            case Action.UP_LEFT: return Action.DOWN_RIGHT;
            case Action.UP_RIGHT: return Action.DOWN_LEFT;
            case Action.DOWN_LEFT: return Action.UP_RIGHT;
            case Action.DOWN_RIGHT: return Action.UP_LEFT;
            default: return Action.ABSORB;
        }
    }
    private int transition(int s, Action a)
    {
        Vector2Int grid_cord = state_to_cord(s);
        Vector2Int next_grid_cord = grid_cord;
        switch(a)
        {
            case Action.ABSORB: return s;
            case Action.LEFT: next_grid_cord.x--; break;
            case Action.RIGHT: next_grid_cord.x++; break;
            case Action.DOWN: next_grid_cord.y--; break;
            case Action.UP: next_grid_cord.y++; break;
            case Action.UP_LEFT:
                next_grid_cord.x--; next_grid_cord.y++;
                break;
            case Action.UP_RIGHT:
                next_grid_cord.x++; next_grid_cord.y++;
                break;
            case Action.DOWN_LEFT:
                next_grid_cord.x--; next_grid_cord.y--;
                break;
            case Action.DOWN_RIGHT:
                next_grid_cord.x++; next_grid_cord.y--;
                break;
        }
        if (!validCord(next_grid_cord.x, next_grid_cord.y))
        {
            return s;
        }
        return cord_to_state(next_grid_cord.x, next_grid_cord.y);
    }
}
