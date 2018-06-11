
-----------------------------------------------------------------------------------------
-- Composites
-- PrioritySelector

-- Used for Utility AI, the Priority Selector executes the child with the highest priority 
-- value. If it fails, the Prioerity Selector will continue with the next highest priority 
-- child until one Succeeds, or until all Fail (similar to how a normal Selector does).
-----------------------------------------------------------------------------------------

local PrioritySelector = bt.Class("PrioritySelector",bt.BTComposite)
bt.PrioritySelector = PrioritySelector

function PrioritySelector:ctor()
    bt.BTComposite.ctor(self)
    self.name = "PrioritySelector"
    self.priorities = {}
    self.current = 1
end

function PrioritySelector:init(jsonData)
    if jsonData.priorities then
        for i=1,#jsonData.priorities do
            table.insert(self.priorities,jsonData.priorities[i]._value)
        end
    end
end

function PrioritySelector:onExecute(agent,blackboard)
    if self.status == bt.Status.Resting then
        table.sort(self.outConnections, function(a,b)
            return self.priorities[a.id] > self.priorities[b.id]
        end)
    end
    for i = self.current, #self.outConnections do
        self.status = self.outConnections[i]:execute(agent,blackboard)
        if self.status == bt.Status.Success then
            return bt.Status.Success
        end
        if self.status == bt.Status.Running then
            self.current = i
            return bt.Status.Running
        end
    end
    return bt.Status.Failure
end

function PrioritySelector:onReset()
    self.current = 1
end

function PrioritySelector:destroy()
    self.priorities = nil
end