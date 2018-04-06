
-----------------------------------------------------------------------------------------
-- Composites
-- StepIterator

-- Executes AND immediately returns children node status ONE-BY-ONE. Step Sequencer always 
-- moves forward by one and loops it's index
-----------------------------------------------------------------------------------------

local StepIterator = bt.Class("StepIterator",bt.BTComposite)
bt.StepIterator = StepIterator

function StepIterator:ctor()
    bt.BTComposite.ctor(self)
    self.name = "StepIterator"
    self.current = 0
end

function StepIterator:init(jsonData)
end

function StepIterator:onExecute(agent,blackboard)
    if self.current > #self.outConnections then
        self.current = 1
    end
    return self.outConnections[self.current]:execute(agent,blackboard)
end

function StepIterator:onReset()
    self.current = self.current + 1
end

function StepIterator:onGraphStarted()
    self.current = 1
end